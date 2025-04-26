using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaskGeniusApi.DTOs.Genius;

namespace TaskGeniusApi.Services.Genius
{
    public class GeniusService : IGeniusService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiApiSettings _settings;
        private readonly ILogger<GeniusService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public GeniusService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ILogger<GeniusService> logger)
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

        private void ValidateRequest(TaskAdviceRequestDto requestDto)
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

        private GeminiRequest BuildRequestContent(TaskAdviceRequestDto requestDto)
        {
            var promptText = new StringBuilder("Dame un consejo sobre cómo organizarme mejor con estas tareas:\n");
            
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
                Contents = new[]
                {
                    new ContentItem
                    {
                        Role = "user",
                        Parts = new[] { new TextPart { Text = promptText.ToString() } }
                    }
                },
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
            return $"{_settings.BaseUrl}{_settings.Model}?key={_settings.ApiKey}";
        }

        private async Task EnsureSuccessResponse(HttpResponseMessage response, string url, string requestPayload)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
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