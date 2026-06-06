using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using INZYNIERKA.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace INZYNIERKA.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly IChatService<User> chatService;
        private readonly IChatAiService<User> chatAiService;
        private readonly ILogger<ChatController> logger;

        public ChatController(UserManager<User> userManager, IChatService<User> chatService, IChatAiService<User> chatAiService, ILogger<ChatController> logger)
        {
            this.userManager = userManager;
            this.chatService = chatService;
            this.chatAiService = chatAiService;
            this.logger = logger;
        }

        // Chat Service //

        [HttpGet]
        public async Task<IActionResult> Chat(string friendId)
        {
            if (string.IsNullOrEmpty(friendId))
            {
                logger.LogWarning("Chat access denied: friendId is null or empty.");
                TempData["ErrorMessage"] = "Wrong friend ID.";
                return RedirectToAction("Index", "Home");
            }

            var userId = userManager.GetUserId(User);

            try
            {
                var userMessage = TempData["UserMessage"]?.ToString() ?? "";
                var geminiAnswer = TempData["GeminiAnswer"]?.ToString() ?? "";
                
                var model = await chatService.Chat(userId, friendId, userMessage, geminiAnswer);

                if(model == null)
                {
                    logger.LogWarning("Chat initialization failed: User {UserId} does not have access or friend {FriendId} not found.", userId, friendId);
                    TempData["ErrorMessage"] = "User not found or you do not have access to chat.";
                    return RedirectToAction("Index", "Home");
                }

                logger.LogInformation("User {UserId} successfully loaded chat with friend {FriendId}.", userId, friendId);

                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load chat for user {UserId} with friend {FriendId}.", userId, friendId);
                TempData["ErrorMessage"] = "Failed to load chat.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> LoadOlderMessages(string friendId, int skip)
        {
            if (string.IsNullOrEmpty(friendId))
            {
                logger.LogWarning("LoadOlderMessages failed: friendId is empty.");
                return Json(new List<object>());
            }

            var userId = userManager.GetUserId(User);

            try
            {
                var olderMessages = await chatService.OlderMessages(userId, friendId, skip);
                return Json(olderMessages);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load older messages for user {UserId} and friend {FriendId} (Skip: {Skip}).", userId, friendId, skip);
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GroupChat(int groupId)
        {
            if (groupId <= 0)
            {
                logger.LogWarning("GroupChat access denied: Invalid groupId ({GroupId}).", groupId);
                TempData["ErrorMessage"] = "Wrong group ID.";
                return RedirectToAction("Index", "Home");
            }

            var userId = userManager.GetUserId(User);

            try
            {
                var userMessage = TempData["UserMessage"]?.ToString() ?? "";
                var geminiAnswer = TempData["GeminiAnswer"]?.ToString() ?? "";

                var model = await chatService.GroupChat(userId, groupId, userMessage, geminiAnswer);
                if (model == null)
                {
                    logger.LogWarning("GroupChat initialization failed: User {UserId} not found in group {GroupId} or group doesn't exist.", userId, groupId);
                    TempData["ErrorMessage"] = "Group not found or you do not have access to chat.";
                    return RedirectToAction("Index", "Home");
                }

                logger.LogInformation("User {UserId} successfully loaded group chat {GroupId}.", userId, groupId);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load group chat {GroupId} for user {UserId}.", groupId, userId);
                TempData["ErrorMessage"] = "Failed to load group chat.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> LoadOlderGroupMessages(int groupId, int skip)
        {
            if (groupId <= 0)
            {
                logger.LogWarning("LoadOlderGroupMessages failed: Invalid groupId ({GroupId}).", groupId);
                return Json(new List<object>());
            }

            var userId = userManager.GetUserId(User);

            try
            {
                var olderMessages = await chatService.OlderGroupMessages(groupId, skip);
                return Json(olderMessages);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load older group messages for group {GroupId} requested by user {UserId} (Skip: {Skip}).", groupId, userId, skip);
                return Json(new List<object>());
            }
        }

        // Chat AI Service //


        [HttpPost]
        public async Task<IActionResult> CorrectMessage(ChatViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.FriendId))
            {
                logger.LogWarning("CorrectMessage failed: Invalid model or missing FriendId.");
                TempData["ErrorMessage"] = "Wrong friend ID.";
                return RedirectToAction("Index", "Home");
            }

            var userId = userManager.GetUserId(User);

            try
            {
                logger.LogInformation("AI successfully corrected a message for user {UserId}.", userId);
                TempData["UserMessage"] = await chatAiService.CorrectMessage(model.UserMessage);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AI message correction failed for user {UserId}.", userId);
                TempData["UserMessage"] = model.UserMessage;
                TempData["ErrorMessage"] = "Error during correcting message.";
            }

            return RedirectToAction("Chat", new { friendId = model.FriendId });
        }

        [HttpPost]
        public async Task<IActionResult> TranslateMessage(ChatViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.FriendId))
            {
                logger.LogWarning("TranslateMessage failed: Invalid model or missing FriendId.");
                TempData["ErrorMessage"] = "Chat session expired.";
                return RedirectToAction("Index", "Home");
            }

            var userId = userManager.GetUserId(User);

            try
            {
                logger.LogInformation("AI successfully translated a message for user {UserId} in chat with {FriendId}.", userId, model.FriendId);
                TempData["UserMessage"] = await chatAiService.TranslateMessage(userId, model.FriendId, model.UserMessage);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AI message translation failed for user {UserId} in chat with {FriendId}.", userId, model.FriendId);
                TempData["UserMessage"] = model.UserMessage;
                TempData["ErrorMessage"] = "Error during translating message.";
            }

            return RedirectToAction("Chat", new {friendId = model.FriendId});
        }

        [HttpPost]
        public async Task<IActionResult> GroupResponseHelp(GroupChatViewModel model)
        {
            if (model == null || model.GroupId <= 0)
            {
                logger.LogWarning("GroupResponseHelp failed: Invalid model or GroupId.");
                TempData["ErrorMessage"] = "Group session error";
                return RedirectToAction("Index", "Home");
            }

            var userId = userManager.GetUserId(User);

            try
            {
                logger.LogInformation("AI successfully generated response help for user {UserId} in group {GroupId}.", userId, model.GroupId);
                TempData["GeminiAnswer"] = await chatAiService.GroupResponseHelp(userId, model.GroupId);
                TempData["UserMessage"] = model.UserMessage;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AI response help failed for user {UserId} in group {GroupId}.", userId, model.GroupId);
                TempData["UserMessage"] = model.UserMessage;
                TempData["ErrorMessage"] = "Error during getting response help";
            }

            return RedirectToAction("GroupChat", new {groupId = model.GroupId});
        }

        [HttpPost]
        public async Task<IActionResult> GroupCorrectMessage(GroupChatViewModel model)
        {
            if (model == null || model.GroupId <= 0)
            {
                logger.LogWarning("GroupCorrectMessage failed: Invalid model or GroupId.");
                TempData["ErrorMessage"] = "Group session error";
                return RedirectToAction("Index", "Home");
            }

            var userId = userManager.GetUserId(User);

            try
            {
                logger.LogInformation("AI successfully corrected a group message for user {UserId} in group {GroupId}.", userId, model.GroupId);
                TempData["UserMessage"] = await chatAiService.CorrectMessage(model.UserMessage);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "AI group message correction failed for user {UserId} in group {GroupId}.", userId, model.GroupId);
                TempData["UserMessage"] = model.UserMessage;
                TempData["ErrorMessage"] = "Error during correcting message.";
            }
            return RedirectToAction("GroupChat", new {groupId = model.GroupId});
        }

        [HttpPost]
        public async Task<IActionResult> GroupTranslateMessage(GroupChatViewModel model)
        {
            if (model == null || model.GroupId <= 0)
            {
                logger.LogWarning("GroupTranslateMessage failed: Invalid model or GroupId.");
                TempData["ErrorMessage"] = "Group session error";
                return RedirectToAction("Index", "Home");
            }

            var userId = userManager.GetUserId(User);

            try
            {
                logger.LogInformation("AI successfully translated a group message for user {UserId} in group {GroupId}.", userId, model.GroupId);
                TempData["UserMessage"] = await chatAiService.TranslateGroupMessage(model.GroupId, model.UserMessage);
            }
            catch (Exception ex) 
            {
                logger.LogError(ex, "AI group message translation failed for user {UserId} in group {GroupId}.", userId, model.GroupId);
                TempData["UserMessage"] = model.UserMessage;
                TempData["ErrorMessage"] = "Error during translating message.";
            }

            return RedirectToAction("GroupChat", new {groupId = model.GroupId});
        }
    }
}