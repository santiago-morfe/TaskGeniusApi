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

        // Refactorización de métodos para mejorar la legibilidad y el control de errores
        public async Task<TaskAdviceResponseDto> GetAdviceAsync(TaskAdviceRequestDto requestDto)
        {
            ValidateRequest(requestDto);
            var url = BuildRequestUrl();
            var jsonPayload = SerializeRequest(BuildRequestContent(requestDto));

            try
            {
                var response = await SendPostRequestAsync(url, jsonPayload);
                var apiResponse = DeserializeResponse<GeminiApiResponse>(response);

                return BuildResponse(apiResponse);
            }
            catch (Exception ex)
            {
                HandleException(ex, url);
                throw;
            }
        }

        public async Task<TitleSuggestionResponseDto> GetTitleSuggestionAsync(string taskDescription)
        {
            ValidateDescription(taskDescription);
            var prompt = CreatePromptForTitleSuggestion(taskDescription);
            var url = BuildRequestUrl();
            var jsonPayload = SerializeRequest(CreateGeminiRequest(prompt, 50));

            try
            {
                var response = await SendPostRequestAsync(url, jsonPayload);
                var apiResponse = DeserializeResponse<GeminiApiResponse>(response);

                return ExtractTitleFromResponse(apiResponse);
            }
            catch (Exception ex)
            {
                HandleException(ex, url);
                throw;
            }
        }

        public async Task<DescriptionFormattingResponseDto> GetDescriptionFormattingAsync(string taskDescription)
        {
            ValidateDescription(taskDescription);
            var prompt = CreatePromptForDescriptionFormatting(taskDescription);
            var url = BuildRequestUrl();
            var jsonPayload = SerializeRequest(CreateGeminiRequest(prompt, 100));

            try
            {
                var response = await SendPostRequestAsync(url, jsonPayload);
                var apiResponse = DeserializeResponse<GeminiApiResponse>(response);

                return ExtractDescriptionFromResponse(apiResponse);
            }
            catch (Exception ex)
            {
                HandleException(ex, url);
                throw;
            }
        }

        // Métodos auxiliares para mejorar la legibilidad
        private void ValidateDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Task description cannot be null or empty", nameof(description));
            }
        }

        private string SerializeRequest<T>(T request)
        {
            return JsonSerializer.Serialize(request, _jsonOptions);
        }

        private async Task<string> SendPostRequestAsync(string url, string jsonPayload)
        {
            using var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(url, httpContent);
            await EnsureSuccessResponse(response, url, jsonPayload);
            return await response.Content.ReadAsStringAsync();
        }

        private T DeserializeResponse<T>(string responsePayload)
        {
            return JsonSerializer.Deserialize<T>(responsePayload, _jsonOptions) ?? throw new GeniusServiceException("Failed to deserialize response");
        }

        private void HandleException(Exception ex, string url)
        {
            _logger.LogError(ex, "Error occurred while processing request to URL: {Url}", url);
        }

        private static string CreatePromptForTitleSuggestion(string taskDescription)
        {
            return $"genera un título breve y descriptivo para la siguiente tarea: \"{taskDescription}\" deve ser una sola frase y contesta de forma brebe solo con la la sugerencia del titulo que deve ser una sola y nada.";
        }

        private static string CreatePromptForDescriptionFormatting(string taskDescription)
        {
            return $"Formatea la siguiente descripción de tarea para que sea más clara y fácil de entender: \"{taskDescription}\" ( resonde solo con el texto se sigerencia y nada mas, no hagreges nada ni formato ni sugerencias ni consejos ).";
        }

        private static GeminiRequest CreateGeminiRequest(string prompt, int maxOutputTokens)
        {
            return new GeminiRequest
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
                    MaxOutputTokens = maxOutputTokens,
                    TopP = 0.9,
                    TopK = 40
                }
            };
        }

        private static TitleSuggestionResponseDto ExtractTitleFromResponse(GeminiApiResponse apiResponse)
        {
            var title = apiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim();
            return new TitleSuggestionResponseDto { Title = title ?? "No se pudo generar un título." };
        }

        private static DescriptionFormattingResponseDto ExtractDescriptionFromResponse(GeminiApiResponse apiResponse)
        {
            var description = apiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim();
            return new DescriptionFormattingResponseDto { Description = description ?? "No se pudo formatear la descripción." };
        }

        private static void ValidateRequest(TaskAdviceRequestDto requestDto)
        {
            ArgumentNullException.ThrowIfNull(requestDto);

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
                        Parts = new[] { new TextPart { Text = promptText.ToString() } }
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