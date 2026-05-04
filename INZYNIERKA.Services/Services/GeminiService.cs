using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using INZYNIERKA.Services.Interfaces;

namespace INZYNIERKA.Services.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly IConfiguration configuration;
        private readonly string apiKey;
        private readonly HttpClient httpClient;

        public GeminiService(IConfiguration configuration, HttpClient httpClient)
        {
            this.configuration = configuration;
            this.apiKey = configuration["ApiKeys:Gemini"] ?? throw new Exception("No API key found for Gemini.");
            this.httpClient = httpClient;
        }

        public async Task<string> AskAsync(string question, string prompt)
        {
            string endpoint = configuration["EndPoints:Gemini"].Replace("{apiKey}", apiKey) ?? throw new Exception("No endpoint configured for Gemini.");

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
                var response = await httpClient.PostAsync(endpoint, content);

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
                Console.WriteLine("[Gemini Warning] Pusta odpowiedź.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gemini Exception]: {ex.Message}");
                return null;
            }
        }
    }
}
