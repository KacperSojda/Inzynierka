using System.Text.Json;
using System.Text;
using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using INZYNIERKA.Services;

namespace INZYNIERKA.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly INZDbContext context;
        private readonly UserManager<User> userManager;
        private readonly GeminiService geminiService;
        private readonly IConfiguration configuration;
        private readonly IChatService chatService;

        public ChatController(UserManager<User> userManager, INZDbContext dbcontext, GeminiService geminiService, IConfiguration configuration, IChatService chatService)
        {
            this.userManager = userManager;
            this.context = dbcontext;
            this.geminiService = geminiService;
            this.configuration = configuration;
            this.chatService = chatService;
        }

        // Chat Service //

        [HttpGet]
        public async Task<IActionResult> Chat(string friendId)
        {
            var userMessage = TempData["UserMessage"]?.ToString() ?? "";
            var geminiAnswer = TempData["GeminiAnswer"]?.ToString() ?? "";

            var model = await chatService.GetPrivateChatAsync(userManager.GetUserId(User), friendId, userMessage, geminiAnswer);

            if (model == null) return NotFound("Nie znaleziono użytkownika.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GroupChat(int groupId)
        {
            var userMessage = TempData["UserMessage"]?.ToString() ?? "";
            var geminiAnswer = TempData["GeminiAnswer"]?.ToString() ?? "";

            var model = await chatService.GetGroupChatAsync(userManager.GetUserId(User), groupId, userMessage, geminiAnswer);

            if (model == null) return NotFound("Nie znaleziono grupy.");
            return View(model);
        }

        // Prywatny Chat //

        [HttpPost]
        public async Task<IActionResult> ResponseHelp(ChatViewModel model)
        {
            var user = await userManager.GetUserAsync(User);

            var messages = await context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m =>
                    (m.SenderId == user.Id && m.ReceiverId == model.FriendId) ||
                    (m.SenderId == model.FriendId && m.ReceiverId == user.Id))
                .OrderByDescending(m => m.DateTime)
                .Take(30)
                .ToListAsync();

            messages.Reverse();

            var messageString = string.Join(", ",
                messages.Select(m =>
                    (m.SenderId == user.Id ? "[user]" : $"[{m.Sender.UserName}]") + " " + m.Content
                )
            );

            string ResponseHelpPrompt = configuration["Prompts:ResponseHelp"];

            string ans = await geminiService.AskAsync(messageString, ResponseHelpPrompt);
            TempData["GeminiAnswer"] = ans;
            TempData["UserMessage"] = model.UserMessage;

            return RedirectToAction("Chat", new {friendId = model.FriendId});
        }

        [HttpPost]
        public async Task<IActionResult> CorrectMessage(ChatViewModel model)
        {
            string CorrectMessagePrompt = configuration["Prompts:CorrectMessage"];

            string ans = await geminiService.AskAsync(model.UserMessage, CorrectMessagePrompt);

            TempData["UserMessage"] = ans;

            return RedirectToAction("Chat", new {friendId = model.FriendId});
        }

        [HttpPost]
        public async Task<IActionResult> TranslateMessage(ChatViewModel model)
        {
            var user = await userManager.GetUserAsync(User);

            var messages = await context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m =>
                    (m.SenderId == user.Id && m.ReceiverId == model.FriendId) ||
                    (m.SenderId == model.FriendId && m.ReceiverId == user.Id))
                .OrderByDescending(m => m.DateTime)
                .Take(30)
                .ToListAsync();

            messages.Reverse();

            string messagestoString = string.Join(", ", messages.Select(m => m.Content));

            string LanguagePrompt = configuration["Prompts:Language"];

            string language = await geminiService.AskAsync(messagestoString, LanguagePrompt);

            string TranslatePrompt = configuration["Prompts:Translate"].Replace("{language}", language);

            string ans = await geminiService.AskAsync(model.UserMessage, TranslatePrompt);

            TempData["UserMessage"] = ans;

            return RedirectToAction("Chat", new {friendId = model.FriendId});
        }

        // Chat Grupowy //

        [HttpPost]
        public async Task<IActionResult> GroupResponseHelp(GroupChatViewModel model)
        {
            var user = await userManager.GetUserAsync(User);

            var lastMessages = await context.GroupMessages
                .Include(m => m.Sender)
                .Where(m => m.GroupId == model.groupID)
                .OrderByDescending(m => m.Timestamp)
                .Take(30)
                .ToListAsync();

            var messageString = string.Join(", ",
                lastMessages.Select(m =>
                    (m.SenderId == user.Id ? "[user]" : $"[{m.Sender.UserName}]") + " " + m.Content)
            );

            string ResponseHelpPrompt = configuration["Prompts:ResponseHelp"];

            string ans = await geminiService.AskAsync(messageString, ResponseHelpPrompt);

            TempData["GeminiAnswer"] = ans;
            TempData["UserMessage"] = model.UserMessage;

            return RedirectToAction("GroupChat", new {groupId = model.groupID});
        }

        [HttpPost]
        public async Task<IActionResult> GroupCorrectMessage(GroupChatViewModel model)
        {
            string CorrectMessagePrompt = configuration["Prompts:CorrectMessage"];

            string ans = await geminiService.AskAsync(model.UserMessage, CorrectMessagePrompt);

            TempData["UserMessage"] = ans;

            return RedirectToAction("GroupChat", new {groupId = model.groupID});
        }

        [HttpPost]
        public async Task<IActionResult> GroupTranslateMessage(GroupChatViewModel model)
        {
            var lastMessages = await context.GroupMessages
                .Where(m => m.GroupId == model.groupID)
                .OrderByDescending(m => m.Timestamp)
                .Take(30)
                .Select(m => m.Content)
                .ToListAsync();

            string messagesToString = string.Join(", ", lastMessages);

            string LanguagePrompt = configuration["Prompts:Language"];

            string language = await geminiService.AskAsync(messagesToString, LanguagePrompt);

            string TranslatePrompt = configuration["Prompts:Translate"].Replace("{language}", language);

             var ans = await geminiService.AskAsync(model.UserMessage, TranslatePrompt);

            TempData["UserMessage"] = ans;

            return RedirectToAction("GroupChat", new {groupId = model.groupID});
        }
    }
}