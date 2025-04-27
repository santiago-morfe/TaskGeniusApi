using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskGeniusApi.DTOs.Genius;

namespace TaskGeniusApi.Services.Genius
{
    public class GeniusService : IGeniusService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiApiSettings _settings;
        private readonly ILogger<GeniusService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public GeniusService(HttpClient httpClient, IConfiguration configuration, ILogger<GeniusService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _settings = configuration.GetSection("gemini").Get<GeminiApiSettings>() ?? 
                throw new InvalidOperationException("Gemini API configuration is not properly set up");
            
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutInSeconds);

            // Configuración de serialización JSON
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };
        }

        public async Task<TaskAdviceResponseDto> GetAdviceAsync(TaskAdviceRequestDto requestDto)
        {
            ValidateRequest(requestDto);
            
            var requestContent = BuildRequestContent(requestDto);
            var url = BuildRequestUrl();

            _logger.LogDebug("Sending request to Gemini API. URL: {Url}", url);

            try
            {
                var jsonPayload = JsonSerializer.Serialize(requestContent, _jsonOptions);
                _logger.LogDebug("Request payload: {Payload}", jsonPayload);

                using var httpContent = new StringContent(
                    jsonPayload, 
                    Encoding.UTF8, 
                    "application/json");

                using var response = await _httpClient.PostAsync(url, httpContent);
                
                await EnsureSuccessResponse(response, url, jsonPayload);
                
                var payload = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("API Response: {Response}", payload);

                var apiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(payload, _jsonOptions);

                if (apiResponse == null)
                {
                    _logger.LogWarning("Gemini API response is null");
                    return new TaskAdviceResponseDto { Advice = "No se pudo obtener un consejo." };
                }

                return BuildResponse(apiResponse);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed for URL: {Url}", url);
                throw new GeniusServiceException("Failed to communicate with Gemini API", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize Gemini API response");
                throw new GeniusServiceException("Invalid response from Gemini API", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing the request");
                throw;
            }
        }

        public async Task<TitleSuggestionResponseDto> GetTitleSuggestionAsync(string taskDescription)
        {
            if (string.IsNullOrWhiteSpace(taskDescription))
            {
                throw new ArgumentException("Task description cannot be null or empty", nameof(taskDescription));
            }

            var prompt = $"Sugiere un título breve y descriptivo para la siguiente tarea: \"{taskDescription}\".";

            var request = new GeminiRequest
            {
                Contents = new[]
                {
                    new ContentItem
                    {
                        Role = "user",
                        Parts = new[] { new TextPart { Text = prompt } }
                    }
                },
                GenerationConfig = new GenerationConfig
                {
                    Temperature = 0.7,
                    MaxOutputTokens = 50,
                    TopP = 0.9,
                    TopK = 40
                }
            };

            var url = BuildRequestUrl();

            try
            {
                var jsonPayload = JsonSerializer.Serialize(request, _jsonOptions);
                using var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync(url, httpContent);

                await EnsureSuccessResponse(response, url, jsonPayload);

                var payload = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(payload, _jsonOptions);

                if (apiResponse?.Candidates == null || apiResponse.Candidates.Length == 0)
                {
                    return new TitleSuggestionResponseDto { Title = "No se pudo generar un título." };
                }

                var title = apiResponse.Candidates[0].Content.Parts[0].Text?.Trim();
                return new TitleSuggestionResponseDto { Title = title ?? "No se pudo generar un título." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating title suggestion");
                throw new GeniusServiceException("Failed to generate title suggestion", ex);
            }
        }

        public async Task<DescriptionFormattingResponseDto> GetDescriptionFormattingAsync(string taskDescription)
        {
            if (string.IsNullOrWhiteSpace(taskDescription))
            {
                throw new ArgumentException("Task description cannot be null or empty", nameof(taskDescription));
            }

            var prompt = $"Formatea la siguiente descripción de tarea para que sea más clara y fácil de entender: \"{taskDescription}\".";

            var request = new GeminiRequest
            {
                Contents = new[]
                {
                    new ContentItem
                    {
                        Role = "user",
                        Parts = new[] { new TextPart { Text = prompt } }
                    }
                },
                GenerationConfig = new GenerationConfig
                {
                    Temperature = 0.7,
                    MaxOutputTokens = 100,
                    TopP = 0.9,
                    TopK = 40
                }
            };

            var url = BuildRequestUrl();

            try
            {
                var jsonPayload = JsonSerializer.Serialize(request, _jsonOptions);
                using var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync(url, httpContent);

                await EnsureSuccessResponse(response, url, jsonPayload);

                var payload = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(payload, _jsonOptions);

                if (apiResponse?.Candidates == null || apiResponse.Candidates.Length == 0)
                {
                    return new DescriptionFormattingResponseDto { FormattedDescription = "No se pudo formatear la descripción." };
                }

                var formattedDescription = apiResponse.Candidates[0].Content.Parts[0].Text?.Trim();
                return new DescriptionFormattingResponseDto { FormattedDescription = formattedDescription ?? "No se pudo formatear la descripción." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting task description");
                throw new GeniusServiceException("Failed to format task description", ex);
            }
        }
        private static void ValidateRequest(TaskAdviceRequestDto requestDto)
        {
            if (requestDto == null)
            {
                throw new ArgumentNullException(nameof(requestDto));
            }

            if (requestDto.Tasks == null || requestDto.Tasks.Count == 0)
            {
                throw new ArgumentException("Task list cannot be empty", nameof(requestDto.Tasks));
            }
        }

        private static GeminiRequest BuildRequestContent(TaskAdviceRequestDto requestDto)
        {
            var promptText = new StringBuilder("Dame un consejo sobre breve de nos mas de 30 palabras de cómo organizarme mejor con estas tareas:\n");
            
            foreach (var task in requestDto.Tasks)
            {
                promptText.AppendLine($"- {task.Title}: {task.Description}");
                
                if (task.DueDate != default)
                {
                    promptText.AppendLine($"(Fecha límite: {task.DueDate:dd/MM/yyyy})");
                }
            }

            return new GeminiRequest
            {
                Contents =
                [
                    new ContentItem
                    {
                        Role = "user",
                        Parts = [new TextPart { Text = promptText.ToString() }]
                    }
                ],
                GenerationConfig = new GenerationConfig
                {
                    Temperature = 0.7,
                    MaxOutputTokens = 500,
                    TopP = 0.9,
                    TopK = 40
                }
            };
        }

        private string BuildRequestUrl()
        {
            return GeniusServiceUtils.BuildRequestUrl(_settings);
        }

        private async Task EnsureSuccessResponse(HttpResponseMessage response, string url, string requestPayload)
        {
            await GeniusServiceUtils.EnsureSuccessResponse(response, url, requestPayload, _logger);
        }

        private TaskAdviceResponseDto BuildResponse(GeminiApiResponse apiResponse)
        {
            if (apiResponse?.Candidates == null || apiResponse.Candidates.Length == 0)
            {
                _logger.LogWarning("Gemini API returned empty response");
                return new TaskAdviceResponseDto { Advice = "No se pudo obtener un consejo." };
            }

            var firstCandidate = apiResponse.Candidates[0];
            
            if (firstCandidate.Content?.Parts == null || firstCandidate.Content.Parts.Length == 0)
            {
                _logger.LogWarning("Gemini API response missing content parts");
                return new TaskAdviceResponseDto { Advice = "No se pudo obtener un consejo." };
            }

            var adviceText = firstCandidate.Content.Parts[0].Text?.Trim();

            return string.IsNullOrWhiteSpace(adviceText)
                ? new TaskAdviceResponseDto { Advice = "No se pudo obtener un consejo." }
                : new TaskAdviceResponseDto { Advice = adviceText };
        }
    }

    public static class GeniusServiceUtils
    {
        public static string BuildRequestUrl(GeminiApiSettings settings)
        {
            return $"{settings.BaseUrl}{settings.Model}?key={settings.ApiKey}";
        }

        public static async Task EnsureSuccessResponse(HttpResponseMessage response, string url, string requestPayload, ILogger logger)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError(
                    "Gemini API request failed. Status: {StatusCode}, URL: {Url}\nRequest: {RequestPayload}\nResponse: {ErrorResponse}", 
                    response.StatusCode, 
                    url,
                    requestPayload,
                    errorContent);

                throw response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => new GeniusServiceException("Invalid API key"),
                    HttpStatusCode.BadRequest => new GeniusServiceException("Invalid request to Gemini API. Check the request structure."),
                    HttpStatusCode.TooManyRequests => new GeniusServiceException("API rate limit exceeded"),
                    _ => new GeniusServiceException($"Request failed with status code {response.StatusCode}")
                };
            }
        }
    }

    public class GeminiApiSettings
    {
        [JsonPropertyName("baseUrl")]
        public string BaseUrl { get; set; } = string.Empty;
        
        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; } = string.Empty;
        
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;
        
        [JsonPropertyName("timeoutInSeconds")]
        public int TimeoutInSeconds { get; set; } = 30;
    }

    public class GeminiRequest
    {
        [JsonPropertyName("contents")]
        public ContentItem[] Contents { get; set; } = Array.Empty<ContentItem>();

        [JsonPropertyName("generationConfig")]
        public GenerationConfig GenerationConfig { get; set; } = new();
    }

    public class ContentItem
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("parts")]
        public TextPart[] Parts { get; set; } = Array.Empty<TextPart>();
    }

    public class TextPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    public class GenerationConfig
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; }

        [JsonPropertyName("topP")]
        public double TopP { get; set; }

        [JsonPropertyName("topK")]
        public int TopK { get; set; }
    }

    public class GeminiApiResponse
    {
        [JsonPropertyName("candidates")]
        public Candidate[] Candidates { get; set; } = Array.Empty<Candidate>();
    }

    public class Candidate
    {
        [JsonPropertyName("content")]
        public Content Content { get; set; } = new();
    }

    public class Content
    {
        [JsonPropertyName("parts")]
        public TextPart[] Parts { get; set; } = Array.Empty<TextPart>();
    }

    public class GeniusServiceException : Exception
    {
        public GeniusServiceException(string message) : base(message) { }
        public GeniusServiceException(string message, Exception innerException) : base(message, innerException) { }
    }
}