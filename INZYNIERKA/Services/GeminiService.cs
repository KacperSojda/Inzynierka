using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace INZYNIERKA.Services
{
    public class GeminiService
    {
        private readonly string apiKey;
        private readonly HttpClient httpClient;

        public GeminiService()
        {
            this.apiKey = "AIzaSyBkEZNxzsKUNIW72EQbmrMcZeOZ0j9FA98";
            this.httpClient = new HttpClient();
        }

        public async Task<string> AskAsync(string question, string help)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return "Pytanie nie może być puste.";
            }

            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";

            var requestBody = new
            {
                contents = new[] {
                    new {
                        parts = new[] {
                            new { text = help },
                            new { text = question }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();

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
