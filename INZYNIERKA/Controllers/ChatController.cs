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
        private readonly UserManager<User> _userManager;
        private readonly IChatService<User> _chatService;
        private readonly IChatAiService<User> _chatAiService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(UserManager<User> userManager, IChatService<User> chatService, IChatAiService<User> chatAiService, ILogger<ChatController> logger)
        {
            this._userManager = userManager;
            this._chatService = chatService;
            this._chatAiService = chatAiService;
            this._logger = logger;
        }

        // Chat Service //

        [HttpGet]
        public async Task<IActionResult> Chat(string friendId)
        {
            if (string.IsNullOrEmpty(friendId))
            {
                _logger.LogWarning("Chat access denied: friendId is null or empty.");
                TempData["ErrorMessage"] = "Wrong friend ID.";
                return RedirectToAction("Index", "Home");
            }

            var userId = _userManager.GetUserId(User);

            try
            {
                var userMessage = TempData["UserMessage"]?.ToString() ?? "";
                var geminiAnswer = TempData["GeminiAnswer"]?.ToString() ?? "";
                
                var model = await _chatService.Chat(userId, friendId, userMessage, geminiAnswer);

                if(model == null)
                {
                    _logger.LogWarning("Chat initialization failed: User {UserId} does not have access or friend {FriendId} not found.", userId, friendId);
                    TempData["ErrorMessage"] = "User not found or you do not have access to chat.";
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogInformation("User {UserId} successfully loaded chat with friend {FriendId}.", userId, friendId);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load chat for user {UserId} with friend {FriendId}.", userId, friendId);
                TempData["ErrorMessage"] = "Failed to load chat.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> LoadOlderMessages(string friendId, int skip)
        {
            if (string.IsNullOrEmpty(friendId))
            {
                _logger.LogWarning("LoadOlderMessages failed: friendId is empty.");
                return Json(new List<object>());
            }

            var userId = _userManager.GetUserId(User);
            try
            {
                var olderMessages = await _chatService.OlderMessages(userId, friendId, skip);
                return Json(olderMessages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load older messages for user {UserId} and friend {FriendId} (Skip: {Skip}).", userId, friendId, skip);
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GroupChat(int groupId)
        {
            if (groupId <= 0)
            {
                _logger.LogWarning("GroupChat access denied: Invalid groupId ({GroupId}).", groupId);
                TempData["ErrorMessage"] = "Wrong group ID.";
                return RedirectToAction("Index", "Home");
            }

            var userId = _userManager.GetUserId(User);

            try
            {
                var userMessage = TempData["UserMessage"]?.ToString() ?? "";
                var geminiAnswer = TempData["GeminiAnswer"]?.ToString() ?? "";

                var model = await _chatService.GroupChat(userId, groupId, userMessage, geminiAnswer);
                if (model == null)
                {
                    _logger.LogWarning("GroupChat initialization failed: User {UserId} not found in group {GroupId} or group doesn't exist.", userId, groupId);
                    TempData["ErrorMessage"] = "Group not found or you do not have access to chat.";
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogInformation("User {UserId} successfully loaded group chat {GroupId}.", userId, groupId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load group chat {GroupId} for user {UserId}.", groupId, userId);
                TempData["ErrorMessage"] = "Failed to load group chat.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> LoadOlderGroupMessages(int groupId, int skip)
        {
            if (groupId <= 0)
            {
                _logger.LogWarning("LoadOlderGroupMessages failed: Invalid groupId ({GroupId}).", groupId);
                return Json(new List<object>());
            }

            var userId = _userManager.GetUserId(User);
            try
            {
                var olderMessages = await _chatService.OlderGroupMessages(groupId, skip);
                return Json(olderMessages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load older group messages for group {GroupId} requested by user {UserId} (Skip: {Skip}).", groupId, userId, skip);
                return Json(new List<object>());
            }
        }

        // Chat AI Service //


        [HttpPost]
        public async Task<IActionResult> CorrectMessage(ChatViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.FriendId))
            {
                _logger.LogWarning("CorrectMessage failed: Invalid model or missing FriendId.");
                TempData["ErrorMessage"] = "Wrong friend ID.";
                return RedirectToAction("Index", "Home");
            }

            var userId = _userManager.GetUserId(User);

            try
            {
                _logger.LogInformation("AI successfully corrected a message for user {UserId}.", userId);
                TempData["UserMessage"] = await _chatAiService.CorrectMessage(model.UserMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI message correction failed for user {UserId}.", userId);
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
                _logger.LogWarning("TranslateMessage failed: Invalid model or missing FriendId.");
                TempData["ErrorMessage"] = "Chat session expired.";
                return RedirectToAction("Index", "Home");
            }

            var userId = _userManager.GetUserId(User);

            try
            {
                _logger.LogInformation("AI successfully translated a message for user {UserId} in chat with {FriendId}.", userId, model.FriendId);
                TempData["UserMessage"] = await _chatAiService.TranslateMessage(userId, model.FriendId, model.UserMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI message translation failed for user {UserId} in chat with {FriendId}.", userId, model.FriendId);
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
                _logger.LogWarning("GroupResponseHelp failed: Invalid model or GroupId.");
                TempData["ErrorMessage"] = "Group session error";
                return RedirectToAction("Index", "Home");
            }

            var userId = _userManager.GetUserId(User);

            try
            {
                _logger.LogInformation("AI successfully generated response help for user {UserId} in group {GroupId}.", userId, model.GroupId);
                TempData["GeminiAnswer"] = await _chatAiService.GroupResponseHelp(userId, model.GroupId);
                TempData["UserMessage"] = model.UserMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI response help failed for user {UserId} in group {GroupId}.", userId, model.GroupId);
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
                _logger.LogWarning("GroupCorrectMessage failed: Invalid model or GroupId.");
                TempData["ErrorMessage"] = "Group session error";
                return RedirectToAction("Index", "Home");
            }

            var userId = _userManager.GetUserId(User);

            try
            {
                _logger.LogInformation("AI successfully corrected a group message for user {UserId} in group {GroupId}.", userId, model.GroupId);
                TempData["UserMessage"] = await _chatAiService.CorrectMessage(model.UserMessage);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "AI group message correction failed for user {UserId} in group {GroupId}.", userId, model.GroupId);
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
                _logger.LogWarning("GroupTranslateMessage failed: Invalid model or GroupId.");
                TempData["ErrorMessage"] = "Group session error";
                return RedirectToAction("Index", "Home");
            }

            var userId = _userManager.GetUserId(User);

            try
            {
                _logger.LogInformation("AI successfully translated a group message for user {UserId} in group {GroupId}.", userId, model.GroupId);
                TempData["UserMessage"] = await _chatAiService.TranslateGroupMessage(model.GroupId, model.UserMessage);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "AI group message translation failed for user {UserId} in group {GroupId}.", userId, model.GroupId);
                TempData["UserMessage"] = model.UserMessage;
                TempData["ErrorMessage"] = "Error during translating message.";
            }

            return RedirectToAction("GroupChat", new {groupId = model.GroupId});
        }
    }
}