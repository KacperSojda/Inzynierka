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
        public ChatController(UserManager<User> userManager, INZDbContext dbcontext, GeminiService geminiService, IConfiguration configuration)
        {
            this.userManager = userManager;
            this.context = dbcontext;
            this.geminiService = geminiService;
            this.configuration = configuration;
        }

        // Chat Prywatny //

        [HttpGet]
        public async Task<IActionResult> Chat(string friendId)
        {
            var user = await userManager.GetUserAsync(User);
            var friend = await userManager.FindByIdAsync(friendId);

            var messages = await context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m =>
                    (m.SenderId == user.Id && m.ReceiverId == friendId) ||
                    (m.SenderId == friendId && m.ReceiverId == user.Id))
                .OrderByDescending(m => m.DateTime) 
                .Take(30)                           
                .ToListAsync();                     

            messages.Reverse();

            var modelMessages = messages.Select(m => new MessageViewModel
            {
                SenderId = m.SenderId,
                SenderName = m.Sender.UserName,
                ReceiverId = m.ReceiverId,
                ReceiverName = m.Receiver.UserName,
                Content = m.Content,
                DateTime = m.DateTime
            }).ToList();

            var model = new ChatViewModel
            {
                FriendId = friend.Id,
                FriendName = friend.UserName,
                CurrentUserId = user.Id,
                CurrentUserName = user.UserName,
                Messages = modelMessages,
                UserMessage = TempData["UserMessage"]?.ToString() ?? "",
                GeminiAnswer = TempData["GeminiAnswer"]?.ToString() ?? "",
                GeminiQuestion = "",
            };

            return View(model);
        }

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

        [HttpGet]
        public async Task<IActionResult> GroupChat(int groupId)
        {
            var user = await userManager.GetUserAsync(User);

            var group = await context.Groups
                .Where(g => g.Id == groupId)
                .Select(g => new { g.Name })
                .FirstOrDefaultAsync();

            var messages = await context.GroupMessages
                .Include(m => m.Sender)
                .Where(m => (m.GroupId == groupId))
                .OrderByDescending(m => m.Timestamp)
                .Take(30)                           
                .ToListAsync();                      

            messages.Reverse();

            var modelMessages = messages.Select(m => new GroupMessageViewModel
            {
                SenderId = m.SenderId,
                SenderName = m.Sender.UserName,
                Content = m.Content,
                DateTime = m.Timestamp
            }).ToList();

            var model = new GroupChatViewModel
            {
                groupID = groupId,
                groupName = group.Name,
                currentUserID = user.Id,
                messages = modelMessages,
                UserMessage = TempData["UserMessage"]?.ToString() ?? "",
                GeminiAnswer = TempData["GeminiAnswer"]?.ToString() ?? ""
            };

            return View(model);
        }

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