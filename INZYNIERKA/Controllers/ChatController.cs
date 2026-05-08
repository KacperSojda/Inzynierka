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
            if (string.IsNullOrEmpty(friendId)) return RedirectToAction("Index", "Home");

            try
            {
                var userMessage = TempData["UserMessage"]?.ToString() ?? "";
                var geminiAnswer = TempData["GeminiAnswer"]?.ToString() ?? "";
                var userId = userManager.GetUserId(User);

                var model = await chatService.GetPrivateChatAsync(userId, friendId, userMessage, geminiAnswer);
                if (model == null) return NotFound("Cannot find the user.");

                return View(model);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> LoadOlderMessages(string friendId, int skip)
        {
            if (string.IsNullOrEmpty(friendId)) return Json(new List<object>());

            try
            {
                var userId = userManager.GetUserId(User);
                var olderMessages = await chatService.GetOlderPrivateMessagesAsync(userId, friendId, skip);
                return Json(olderMessages);
            }
            catch (Exception ex)
            {
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GroupChat(int groupId)
        {
            if (groupId <= 0) return RedirectToAction("Index", "Home");

            try
            {

                var userMessage = TempData["UserMessage"]?.ToString() ?? "";
                var geminiAnswer = TempData["GeminiAnswer"]?.ToString() ?? "";
                var userId = userManager.GetUserId(User);

                var model = await chatService.GetGroupChatAsync(userId, groupId, userMessage, geminiAnswer);
                if (model == null) return NotFound("Nie znaleziono grupy.");

                return View(model);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> LoadOlderGroupMessages(int groupId, int skip)
        {
            if (groupId <= 0) return Json(new List<object>());

            try
            {
                var olderMessages = await chatService.GetOlderGroupMessagesAsync(groupId, skip);
                return Json(olderMessages);
            }
            catch (Exception ex)
            {
                return Json(new List<object>());
            }
        }

        // Chat AI Service //

        [HttpPost]
        public async Task<IActionResult> ResponseHelp(ChatViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.FriendId)) return RedirectToAction("Index", "Home");

            try
            {
                var userId = userManager.GetUserId(User);
                TempData["GeminiAnswer"] = await chatAiService.GetPrivateResponseHelpAsync(userId, model.FriendId);
                TempData["UserMessage"] = model.UserMessage;
            }
            catch (Exception ex)
            {
                TempData["UserMessage"] = model.UserMessage;
                TempData["GeminiAnswer"] = "Error occurs during helping with response.";
            }

            return RedirectToAction("Chat", new { friendId = model.FriendId });
        }

        [HttpPost]
        public async Task<IActionResult> CorrectMessage(ChatViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.FriendId)) return RedirectToAction("Index", "Home");

            try
            {
                TempData["UserMessage"] = await chatAiService.CorrectMessageAsync(model.UserMessage);
            }
            catch (Exception ex)
            {
                TempData["UserMessage"] = model.UserMessage;
            }

            return RedirectToAction("Chat", new { friendId = model.FriendId });
        }

        [HttpPost]
        public async Task<IActionResult> TranslateMessage(ChatViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.FriendId)) return RedirectToAction("Index", "Home");

            try
            {
                var userId = userManager.GetUserId(User);
                TempData["UserMessage"] = await chatAiService.TranslatePrivateMessageAsync(userId, model.FriendId, model.UserMessage);
            }
            catch (Exception ex)
            {
                TempData["UserMessage"] = model.UserMessage;
            }

            return RedirectToAction("Chat", new {friendId = model.FriendId});
        }

        [HttpPost]
        public async Task<IActionResult> GroupResponseHelp(GroupChatViewModel model)
        {
            if (model == null || model.groupID <= 0) return RedirectToAction("Index", "Home");

            try
            {
                var userId = userManager.GetUserId(User);
                TempData["GeminiAnswer"] = await chatAiService.GetGroupResponseHelpAsync(userId, model.groupID);
                TempData["UserMessage"] = model.UserMessage;
            }
            catch (Exception ex)
            {
                TempData["UserMessage"] = model.UserMessage;
                TempData["GeminiAnswer"] = "Error occurs during helping with response.";
            }

            return RedirectToAction("GroupChat", new {groupId = model.groupID});
        }

        [HttpPost]
        public async Task<IActionResult> GroupCorrectMessage(GroupChatViewModel model)
        {
            if (model == null || model.groupID <= 0) return RedirectToAction("Index", "Home");

            try
            {
                TempData["UserMessage"] = await chatAiService.CorrectMessageAsync(model.UserMessage);

            }
            catch (Exception ex)
            {
                TempData["UserMessage"] = model.UserMessage;
            }
            return RedirectToAction("GroupChat", new { groupId = model.groupID });
        }

        [HttpPost]
        public async Task<IActionResult> GroupTranslateMessage(GroupChatViewModel model)
        {
            if (model == null || model.groupID <= 0) return RedirectToAction("Index", "Home");

            try
            {
                TempData["UserMessage"] = await chatAiService.TranslateGroupMessageAsync(model.groupID, model.UserMessage);
            }
            catch (Exception ex) 
            {
                TempData["UserMessage"] = model.UserMessage;
            }

            return RedirectToAction("GroupChat", new { groupId = model.groupID });
        }

        [HttpPost]
        public async Task<IActionResult> SummarizeChat(ChatViewModel model, int days = 7)
        {
            if (model == null || string.IsNullOrEmpty(model.FriendId)) return RedirectToAction("Index", "Home");

            try
            {
                var userId = userManager.GetUserId(User);

                TempData["GeminiAnswer"] = await chatAiService.SummarizePrivateChatAsync(userId, model.FriendId, days);
            }
            catch (Exception ex)
            {
                TempData["GeminiAnswer"] = "Error occurs during summarizing chat.";
            }

            return RedirectToAction("Chat", new { friendId = model.FriendId });
        }
    }
}