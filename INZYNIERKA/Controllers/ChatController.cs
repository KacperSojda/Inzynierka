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
        private readonly IChatService<User> chatService;
        private readonly IChatAiService<User> chatAiService;

        public ChatController(UserManager<User> userManager, IChatService<User> chatService, IChatAiService<User> chatAiService)
        {
            this.userManager = userManager;
            this.chatService = chatService;
            this.chatAiService = chatAiService;
        }

        // Chat Service //

        [HttpGet]
        public async Task<IActionResult> Chat(string friendId)
        {
            if (string.IsNullOrEmpty(friendId))
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var userMessage = TempData["UserMessage"]?.ToString() ?? "";
                var geminiAnswer = TempData["GeminiAnswer"]?.ToString() ?? "";
                var userId = userManager.GetUserId(User);

                var model = await chatService.Chat(userId, friendId, userMessage, geminiAnswer);

                if(model == null)
                {
                    return NotFound("Cannot find the user.");
                }

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
            if (string.IsNullOrEmpty(friendId))
            {
                return Json(new List<object>());
            }

            try
            {
                var userId = userManager.GetUserId(User);
                var olderMessages = await chatService.OlderMessages(userId, friendId, skip);
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
            if (groupId <= 0)
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var userMessage = TempData["UserMessage"]?.ToString() ?? "";
                var geminiAnswer = TempData["GeminiAnswer"]?.ToString() ?? "";
                var userId = userManager.GetUserId(User);

                var model = await chatService.GroupChat(userId, groupId, userMessage, geminiAnswer);
                if (model == null)
                {
                    return NotFound("Cannot find the group.");
                }

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
                var olderMessages = await chatService.OlderGroupMessages(groupId, skip);
                return Json(olderMessages);
            }
            catch (Exception ex)
            {
                return Json(new List<object>());
            }
        }

        // Chat AI Service //


        [HttpPost]
        public async Task<IActionResult> CorrectMessage(ChatViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.FriendId)) return RedirectToAction("Index", "Home");

            try
            {
                TempData["UserMessage"] = await chatAiService.CorrectMessage(model.UserMessage);
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
            if (model == null || string.IsNullOrEmpty(model.FriendId))
            { 
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var userId = userManager.GetUserId(User);
                TempData["UserMessage"] = await chatAiService.TranslateMessage(userId, model.FriendId, model.UserMessage);
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
            if (model == null || model.GroupId <= 0) return RedirectToAction("Index", "Home");

            try
            {
                var userId = userManager.GetUserId(User);
                TempData["GeminiAnswer"] = await chatAiService.GroupResponseHelp(userId, model.GroupId);
                TempData["UserMessage"] = model.UserMessage;
            }
            catch (Exception ex)
            {
                TempData["UserMessage"] = model.UserMessage;
                TempData["GeminiAnswer"] = "Error during helping with response.";
            }

            return RedirectToAction("GroupChat", new {groupId = model.GroupId});
        }

        [HttpPost]
        public async Task<IActionResult> GroupCorrectMessage(GroupChatViewModel model)
        {
            if (model == null || model.GroupId <= 0)
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                TempData["UserMessage"] = await chatAiService.CorrectMessage(model.UserMessage);

            }
            catch(Exception ex)
            {
                TempData["UserMessage"] = model.UserMessage;
            }
            return RedirectToAction("GroupChat", new {groupId = model.GroupId});
        }

        [HttpPost]
        public async Task<IActionResult> GroupTranslateMessage(GroupChatViewModel model)
        {
            if (model == null || model.GroupId <= 0) return RedirectToAction("Index", "Home");

            try
            {
                TempData["UserMessage"] = await chatAiService.TranslateGroupMessage(model.GroupId, model.UserMessage);
            }
            catch (Exception ex) 
            {
                TempData["UserMessage"] = model.UserMessage;
            }

            return RedirectToAction("GroupChat", new {groupId = model.GroupId});
        }
    }
}