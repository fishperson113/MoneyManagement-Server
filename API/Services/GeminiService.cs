using System.Text.Json;
using System.Text;
using API.Models.DTOs;

namespace API.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _geminiApiKey;
        private readonly string _geminiApiUrl;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<GeminiService> _logger;
        public GeminiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<GeminiService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_KEY")
                                 ?? configuration["GeminiAI:ApiKey"]
                                 ?? throw new InvalidOperationException("Gemini API key not configured");
            _geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_geminiApiKey}";
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> AskGeminiAsync(string userPrompt)
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = userPrompt }
                        }
                    }
                }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(_geminiApiUrl, jsonContent);

            // Read response content regardless of success status
            var responseContent = await response.Content.ReadAsStringAsync();

            // If request failed, throw exception with details
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Gemini API request failed with status code {(int)response.StatusCode}: {responseContent}");
            }

            return responseContent;
        }
        public async Task<TransactionInfo> ExtractTransactionInfoAsync(string ocrText)
        {
            try
            {
                string promptFilePath = Path.Combine(_environment.ContentRootPath, "Prompts", "ExtractTransactionPrompt.txt");
                if (!File.Exists(promptFilePath))
                {
                    throw new FileNotFoundException("Transaction extraction prompt template not found.", promptFilePath);
                }

                string promptTemplate = await File.ReadAllTextAsync(promptFilePath);
                string prompt = promptTemplate.Replace("{{OCR_TEXT}}", ocrText);

                string geminiResponse = await AskGeminiAsync(prompt);
                _logger.LogInformation("Gemini API raw response: {ResponseContent}", geminiResponse);

                using var jsonDoc = JsonDocument.Parse(geminiResponse);
                var textElement = jsonDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text");

                string rawText = textElement.GetString() ?? throw new JsonException("Missing 'text' content in Gemini response");

                string cleanedJson = rawText
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                TransactionInfo? transactionInfo = JsonSerializer.Deserialize<TransactionInfo>(cleanedJson, options);

                if (transactionInfo == null)
                {
                    throw new JsonException("Failed to deserialize transaction information from cleaned JSON.");
                }

                return transactionInfo;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error while extracting transaction info");
                throw new ApplicationException("Failed to parse transaction information from the AI response.", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API request error while extracting transaction info");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while extracting transaction info");
                throw new ApplicationException("An unexpected error occurred while extracting transaction information.", ex);
            }
        }

    }
}
