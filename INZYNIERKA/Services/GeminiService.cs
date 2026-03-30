using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace INZYNIERKA.Services
{
    public class GeminiService
    {
        private readonly IConfiguration configuration;
        private readonly string apiKey;
        private readonly HttpClient httpClient;

        public GeminiService(IConfiguration configuration)
        {
            this.configuration = configuration;
            this.apiKey = configuration["ApiKeys:Gemini"];
            this.httpClient = new HttpClient();
        }

        public async Task<string> AskAsync(string question, string prompt)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return "The question cannot be empty.";
            }

            string endpoint = configuration["EndPoints:Gemini"].Replace("{apiKey}", apiKey);

            var fullPrompt = prompt + question;

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
                    return $"Błąd API (Status: {(int)response.StatusCode}): {errorContent}";
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);

                return doc.RootElement
                          .GetProperty("candidates")[0]
                          .GetProperty("content")
                          .GetProperty("parts")[0]
                          .GetProperty("text")
                          .GetString();
            }
            catch (Exception ex)
            {
                return $"Błąd Gemini: {ex.Message}";
            }
        }
    }
}
