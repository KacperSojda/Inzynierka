using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using INZYNIERKA.Services.Interfaces;

namespace INZYNIERKA.Services.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public GeminiService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _apiKey = _configuration["ApiKeys:Gemini"] ?? throw new Exception("No API key found for Gemini.");
            _httpClient = httpClient;
        }

        public async Task<string> AskAsync(string question, string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt) && string.IsNullOrWhiteSpace(question))
            {
                return string.Empty;
            }

            string endpoint = _configuration["EndPoints:Gemini"].Replace("{apiKey}", _apiKey) ?? throw new Exception("No endpoint configured for Gemini.");

            var fullPrompt = $"{prompt}\n{question}";

            var requestBody = new
            {
                contents = new[] {
                    new {
                        parts = new[] {
                            new {text = fullPrompt},
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[Gemini API Error] {response.StatusCode}: {errorContent}");
                    return null;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);

                var root = doc.RootElement;
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var contentProp) &&
                        contentProp.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        return parts[0].GetProperty("text").GetString()?.Trim();
                    }
                }
                return null;
            }
            catch (Exception ex) 
            { 
                return null; 
            }
        }
    }
}
