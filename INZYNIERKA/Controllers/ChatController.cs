using System.Text.Json;
using System.Text;
using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using INZYNIERKA.Services;

namespace INZYNIERKA.Controllers
{
    public class ChatController : Controller
    {
        private readonly INZDbContext context;
        private readonly UserManager<User> userManager;
        private readonly GeminiService geminiService;
        public ChatController(UserManager<User> userManager, INZDbContext dbcontext, GeminiService geminiService)
        {
            this.userManager = userManager;
            this.context = dbcontext;
            this.geminiService = geminiService;
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
                UserMessage = "",
                GeminiAnswer = "",
                GeminiQuestion = "",
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AskGemini(ChatViewModel model)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            model.GeminiAnswer = await geminiService.AskAsync(model.GeminiQuestion, " ");

            var messages = await context.Messages
                .Where(m =>
                    (m.SenderId == user.Id && m.ReceiverId == model.FriendId) ||
                    (m.SenderId == model.FriendId && m.ReceiverId == user.Id))
                .OrderBy(m => m.DateTime)
                .ToListAsync();

            var updatedModel = new ChatViewModel
            {
                FriendId = model.FriendId,
                CurrentUserId = user.Id,
                Messages = messages,
                UserMessage = model.UserMessage,
                GeminiAnswer = model.GeminiAnswer,
                GeminiQuestion = "",
            };

            return View("Chat", updatedModel);
        }

        [HttpPost]
        public async Task<IActionResult> ResponseHelp(ChatViewModel model)
        {
            var user = await userManager.GetUserAsync(User);

            var messages = await context.Messages
                .Where(m =>
                    (m.SenderId == user.Id && m.ReceiverId == model.FriendId) ||
                    (m.SenderId == model.FriendId && m.ReceiverId == user.Id))
                .OrderBy(m => m.DateTime)
                .ToListAsync();

            var updatedModel = new ChatViewModel
            {
                FriendId = model.FriendId,
                CurrentUserId = user.Id,
                Messages = messages,
                UserMessage = model.UserMessage,
                GeminiAnswer = "",
                GeminiQuestion = "",
            };

            var last30Messages = messages.TakeLast(30).ToList();

            var messageString = string.Join(", ",
                last30Messages.Select(m =>
                    (m.SenderId == user.Id ? "[user]" : "[friend]") + " " + m.Content
                )
            );

            updatedModel.GeminiAnswer = await geminiService.AskAsync(messageString, "You are a helpful chat assistant. Below is a conversation between two people. Your task is to help user in conversation, understand the context of the conversation and suggest a thoughtful and relevant reply to the most recent message. Keep your tone natural and friendly. Your reply should be appropriate to the tone and style of the conversation so far. Do not repeat what has already been said. Do not add unnecessary commentary. Chat history:");

            return View("Chat", updatedModel);
        }

        [HttpPost]
        public async Task<IActionResult> CorrectMessage(ChatViewModel model)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var messages = await context.Messages
                .Where(m =>
                    (m.SenderId == user.Id && m.ReceiverId == model.FriendId) ||
                    (m.SenderId == model.FriendId && m.ReceiverId == user.Id))
                .OrderBy(m => m.DateTime)
                .ToListAsync();

            var updatedModel = new ChatViewModel
            {
                FriendId = model.FriendId,
                CurrentUserId = user.Id,
                Messages = messages,
                UserMessage = model.UserMessage,
                GeminiAnswer = "",
                GeminiQuestion = "",
            };

            updatedModel.UserMessage = await geminiService.AskAsync(model.UserMessage, "You are a professional multilingual proofreader. Correct the following message, fixing spelling, punctuation, and grammar errors, and improving sentence structure for clarity and style. Keep the original meaning and language of the message, Respond only with the corrected message, without explanations or extra text ");

            return View("Chat", updatedModel);
        }

        public async Task<IActionResult> TranslateMessage(ChatViewModel model)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var messages = await context.Messages
                .Where(m =>
                    (m.SenderId == user.Id && m.ReceiverId == model.FriendId) ||
                    (m.SenderId == model.FriendId && m.ReceiverId == user.Id))
                .OrderBy(m => m.DateTime)
                .ToListAsync();

            var updatedModel = new ChatViewModel
            {
                FriendId = model.FriendId,
                CurrentUserId = user.Id,
                Messages = messages,
                UserMessage = model.UserMessage,
                GeminiAnswer = "",
                GeminiQuestion = "",
            };

            var last30Messages = messages.TakeLast(30).ToList();
            string messagestoString = string.Join(", ", last30Messages.Select(m => m.Content));

            string language = await geminiService.AskAsync(messagestoString, "Detect the language of the following message and respond with only the language name in English, without explanations or extra text");

            updatedModel.UserMessage = await geminiService.AskAsync(model.UserMessage, string.Join("Translate the following text to",language,"Respond only with the translated text without any extra explanation"));

            return View("Chat", updatedModel);
        }
    }
}