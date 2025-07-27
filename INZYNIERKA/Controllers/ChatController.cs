using System.Text.Json;
using System.Text;
using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Controllers
{
    public class ChatController : Controller
    {
        private readonly INZDbContext context;
        private readonly UserManager<User> userManager;
        private readonly string apiKey;
        public ChatController(UserManager<User> userManager, INZDbContext dbcontext)
        {
            this.userManager = userManager;
            this.context = dbcontext;
            this.apiKey = "AIzaSyB9zvmt8EiZ2VclEyfypLr9CwROXlQIIdA";
        }

        [HttpGet]
        public async Task<IActionResult> Chat(string friendId)
        {
            var user = await userManager.GetUserAsync(User);

            var messages = await context.Messages
                .Where(m =>
                    (m.SenderId == user.Id && m.ReceiverId == friendId) ||
                    (m.SenderId == friendId && m.ReceiverId == user.Id))
                .OrderBy(m => m.DateTime)
                .ToListAsync();

            var model = new ChatViewModel
            {
                FriendId = friendId,
                CurrentUserId = user.Id,
                Messages = messages,
                GeminiAnswer = "",
                GeminiQuestion = "2+2"
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AskGemini(ChatViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.GeminiQuestion))
            {
                model.GeminiAnswer = "Pytanie nie może być puste.";
                return View("Chat", model);
            }

            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = model.GeminiQuestion }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();

            try
            {
                var response = await httpClient.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);

                model.GeminiAnswer = doc.RootElement
                                        .GetProperty("candidates")[0]
                                        .GetProperty("content")
                                        .GetProperty("parts")[0]
                                        .GetProperty("text")
                                        .GetString();
            }
            catch (Exception ex)
            {
                model.GeminiAnswer = $"Błąd Gemini: {ex.Message}";
            }

            var user = await userManager.GetUserAsync(User);

            var messages = await context.Messages
                .Where(m =>
                    (m.SenderId == user.Id && m.ReceiverId == model.FriendId) ||
                    (m.SenderId == model.FriendId && m.ReceiverId == user.Id))
                .OrderBy(m => m.DateTime)
                .ToListAsync();

            var model2 = new ChatViewModel{
                FriendId = model.FriendId,
                CurrentUserId = model.CurrentUserId,
                Messages = messages,
                GeminiAnswer = model.GeminiAnswer,
                GeminiQuestion = "2+2"
            };

            return View("Chat", model2);
        }
    }
}