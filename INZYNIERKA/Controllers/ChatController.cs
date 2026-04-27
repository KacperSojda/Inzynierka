using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using INZYNIERKA.Services.Interfaces;

namespace INZYNIERKA.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly IChatService chatService;
        private readonly IChatAiService chatAiService;

        public ChatController(UserManager<User> userManager, IChatService chatService, IChatAiService chatAiService)
        {
            this.userManager = userManager;
            this.chatService = chatService;
            this.chatAiService = chatAiService;
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
        public async Task<IActionResult> LoadOlderMessages(string friendId, int skip)
        {
            var userId = userManager.GetUserId(User);
            var olderMessages = await chatService.GetOlderPrivateMessagesAsync(userId, friendId, skip);

            return Json(olderMessages);
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

        [HttpGet]
        public async Task<IActionResult> LoadOlderGroupMessages(int groupId, int skip)
        {
            var olderMessages = await chatService.GetOlderGroupMessagesAsync(groupId, skip);
            return Json(olderMessages);
        }

        // Chat AI Service //

        [HttpPost]
        public async Task<IActionResult> ResponseHelp(ChatViewModel model)
        {
            TempData["GeminiAnswer"] = await chatAiService.GetPrivateResponseHelpAsync(userManager.GetUserId(User), model.FriendId);
            TempData["UserMessage"] = model.UserMessage;
            return RedirectToAction("Chat", new {friendId = model.FriendId});
        }

        [HttpPost]
        public async Task<IActionResult> CorrectMessage(ChatViewModel model)
        {
            TempData["UserMessage"] = await chatAiService.CorrectMessageAsync(model.UserMessage);
            return RedirectToAction("Chat", new {friendId = model.FriendId});
        }

        [HttpPost]
        public async Task<IActionResult> TranslateMessage(ChatViewModel model)
        {
            TempData["UserMessage"] = await chatAiService.TranslatePrivateMessageAsync(userManager.GetUserId(User), model.FriendId, model.UserMessage);
            return RedirectToAction("Chat", new {friendId = model.FriendId});
        }

        [HttpPost]
        public async Task<IActionResult> GroupResponseHelp(GroupChatViewModel model)
        {
            TempData["GeminiAnswer"] = await chatAiService.GetGroupResponseHelpAsync(userManager.GetUserId(User), model.groupID);
            TempData["UserMessage"] = model.UserMessage;
            return RedirectToAction("GroupChat", new {groupId = model.groupID});
        }

        [HttpPost]
        public async Task<IActionResult> GroupCorrectMessage(GroupChatViewModel model)
        {
            TempData["UserMessage"] = await chatAiService.CorrectMessageAsync(model.UserMessage);
            return RedirectToAction("GroupChat", new { groupId = model.groupID });
        }

        [HttpPost]
        public async Task<IActionResult> GroupTranslateMessage(GroupChatViewModel model)
        {
            TempData["UserMessage"] = await chatAiService.TranslateGroupMessageAsync(model.groupID, model.UserMessage);
            return RedirectToAction("GroupChat", new { groupId = model.groupID });
        }
    }
}