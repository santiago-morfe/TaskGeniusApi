using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskGeniusApi.DTOs.Genius;
using TaskGeniusApi.DTOs.Tasks;

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

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };
        }

        public async Task<TaskAdviceResponseDto> GetAdviceAsync(TaskAdviceRequestDto requestDto)
        {
            ValidateTaskAdviceRequest(requestDto);
            
            var tasks = requestDto.Tasks.Select(task => new TaskDto
            {
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate
            }).ToList();
            var prompt = BuildTaskAdvicePrompt(tasks);
            var request = CreateGeminiRequest(prompt, 500);
            
            return await ExecuteGeminiRequestAsync<TaskAdviceResponseDto>(
                request, 
                response => new TaskAdviceResponseDto { 
                    Advice = ExtractTextFromResponse(response, "No se pudo obtener un consejo.") 
                });
        }

        public async Task<TitleSuggestionResponseDto> GetTitleSuggestionAsync(string taskDescription)
        {
            ValidateDescription(taskDescription);
            
            var prompt = $"genera un título breve y descriptivo para la siguiente tarea: \"{taskDescription}\" " +
                         "deve ser una sola frase y contesta de forma brebe solo con la la sugerencia del titulo que deve ser una sola y nadamas, sin formato y en texto plano.";
            
            var request = CreateGeminiRequest(prompt, 50);
            
            return await ExecuteGeminiRequestAsync<TitleSuggestionResponseDto>(
                request, 
                response => new TitleSuggestionResponseDto { 
                    Title = ExtractTextFromResponse(response, "No se pudo generar un título.") 
                });
        }

        public async Task<DescriptionFormattingResponseDto> GetDescriptionFormattingAsync(string taskDescription)
        {
            ValidateDescription(taskDescription);
            
            var prompt = $"Formatea la siguiente descripción de tarea para que sea más clara y fácil de entender: " +
                         $"\"{taskDescription}\" (resonde solo con el texto se sigerencia y nada mas, no hagreges nada ni formato ni sugerencias ni consejos).";
            
            var request = CreateGeminiRequest(prompt, 100);
            
            return await ExecuteGeminiRequestAsync<DescriptionFormattingResponseDto>(
                request, 
                response => new DescriptionFormattingResponseDto { 
                    Description = ExtractTextFromResponse(response, "No se pudo formatear la descripción.") 
                });
        }

        public async Task<TaskAdviceResponseDto> GetAdviceTaskAsync(string taskDescription)
        {
            ValidateDescription(taskDescription);

            var prompt = $"Dame un consejo breve de no más de 40 palabras sobre cómo organizar o ejecutar mejor la siguiente tarea: \"{taskDescription}\".";
            var request = CreateGeminiRequest(prompt, 100);

            return await ExecuteGeminiRequestAsync<TaskAdviceResponseDto>(
                request,
                response => new TaskAdviceResponseDto
                {
                    Advice = ExtractTextFromResponse(response, "No se pudo obtener un consejo.")
                });
        }

        public async Task<TaskAdviceResponseDto> GetTaskQuestionAsync(TaskAdviceRequestDto requestDto, string question)
        {
            ValidateTaskAdviceRequest(requestDto);
            ValidateDescription(question);

            var tasks = requestDto.Tasks.Select(task => new TaskDto
            {
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate
            }).ToList();

            var prompt = new StringBuilder($"Responde la siguiente pregunta sobre mis tareas: \"{question}\".\n");
            prompt.AppendLine("Aquí están las tareas:");
            foreach (var task in tasks)
            {
                prompt.AppendLine($"- {task.Title}: {task.Description}");
                if (task.DueDate != default)
                {
                    prompt.AppendLine($"(Fecha límite: {task.DueDate:dd/MM/yyyy})");
                }
            }

            var request = CreateGeminiRequest(prompt.ToString(), 200);

            return await ExecuteGeminiRequestAsync<TaskAdviceResponseDto>(
                request,
                response => new TaskAdviceResponseDto
                {
                    Advice = ExtractTextFromResponse(response, "No se pudo responder la pregunta.")
                });
        }

        #region Métodos privados auxiliares
        
        private async Task<TResponse> ExecuteGeminiRequestAsync<TResponse>(GeminiRequest request, Func<GeminiApiResponse, TResponse> responseBuilder)
        {
            var url = $"{_settings.BaseUrl}{_settings.Model}?key={_settings.ApiKey}";
            var jsonPayload = JsonSerializer.Serialize(request, _jsonOptions);

            try
            {
                using var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync(url, httpContent);
                
                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(response, url, jsonPayload);
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(responseBody, _jsonOptions);
                
                if (apiResponse == null)
                {
                    throw new GeniusServiceException("Failed to deserialize API response");
                }

                return responseBuilder(apiResponse);
            }
            catch (Exception ex) when (ex is not GeniusServiceException)
            {
                _logger.LogError(ex, "Error occurred while processing request to URL: {Url}", url);
                throw new GeniusServiceException("Error communicating with Gemini API", ex);
            }
        }

        private async Task HandleErrorResponse(HttpResponseMessage response, string url, string requestPayload)
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
                HttpStatusCode.BadRequest => new GeniusServiceException("Invalid request to Gemini API"),
                HttpStatusCode.TooManyRequests => new GeniusServiceException("API rate limit exceeded"),
                _ => new GeniusServiceException($"Request failed with status code {response.StatusCode}")
            };
        }

        private static void ValidateTaskAdviceRequest(TaskAdviceRequestDto requestDto)
        {
            ArgumentNullException.ThrowIfNull(requestDto);

            if (requestDto.Tasks == null || requestDto.Tasks.Count == 0)
            {
                throw new ArgumentException("Task list cannot be empty", nameof(requestDto.Tasks));
            }
        }

        private static void ValidateDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Task description cannot be null or empty", nameof(description));
            }
        }

        private static string BuildTaskAdvicePrompt(List<TaskDto> tasks)
        {
            var promptText = new StringBuilder("Dame un consejo sobre breve de no mas de 40 palabras de cómo organizarme o ejecutar mejor estas tareas:\n");
            
            foreach (var task in tasks)
            {
                promptText.AppendLine($"- {task.Title}: {task.Description}");
                
                if (task.DueDate != default)
                {
                    promptText.AppendLine($"(Fecha límite: {task.DueDate:dd/MM/yyyy})");
                }
            }

            return promptText.ToString();
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

        private static string ExtractTextFromResponse(GeminiApiResponse apiResponse, string defaultMessage)
        {
            return apiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim() ?? defaultMessage;
        }
        
        #endregion
    }

    #region Clases de apoyo

    public class GeminiApiSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
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
    
    #endregion
}