using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace INZYNIERKA.Controllers
{
    [Authorize]
    public class FriendshipController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly IFriendshipService<User> friendshipService;
        private readonly ILogger<FriendshipController> logger;
        public FriendshipController(UserManager<User> userManager, IFriendshipService<User> friendshipService, ILogger<FriendshipController> logger)
        {
            this.userManager = userManager;
            this.friendshipService = friendshipService;
            this.logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> FriendRequestAccept(int notificationId)
        {
            var userId = userManager.GetUserId(User);

            if (notificationId <= 0)
            {
                return RedirectToAction("Notifications");
            }

            try
            {
                var result = await friendshipService.AcceptRequest(userId, notificationId);

                if (!result)
                {
                    logger.LogWarning("User {UserId} failed to accept friend request from notification {NotificationId}.", userId, notificationId);
                    TempData["ErrorMessage"] = "Cannot accept the friend request.";
                }
                else
                {
                    logger.LogInformation("User {UserId} accepted friend request from notification {NotificationId} successfully.", userId, notificationId);
                    TempData["SuccessMessage"] = "Friend request accepted.";
                }

                return RedirectToAction("Notifications");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server error while accepting friend request from notification {NotificationId} for user {UserId}.", notificationId, userId);
                TempData["ErrorMessage"] = "Server error";
                return RedirectToAction("Notifications");
            }
        }


        [HttpGet]
        public async Task<IActionResult> FriendList(string? searchQuery, int page = 1)
        {
            var userId = userManager.GetUserId(User);

            try
            {
                int pageSize = 10;

                var (model, totalCount) = await friendshipService.FriendList(userId, searchQuery, page, pageSize);

                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                logger.LogInformation("User {UserId} accessed their friend list (Page: {Page}).", userId, page);
                return View("FriendList", model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load friend list for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load your friend list.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFriend(string friendId)
        {
            var userId = userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(friendId))
            {
                logger.LogWarning("DeleteFriend failed: Target FriendId is null for user {UserId}.", userId);
                return RedirectToAction("FriendList");
            }

            try
            {
                await friendshipService.DeleteFriend(userId, friendId);

                logger.LogInformation("User {UserId} removed {TargetFriendId} from their friends list.", userId, friendId);
                TempData["SuccessMessage"] = "User has been removed from your friends.";
                return RedirectToAction("FriendList");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove friend {TargetFriendId} for user {UserId}.", friendId, userId);
                TempData["ErrorMessage"] = "Failed to remove friend.";
                return RedirectToAction("FriendList");
            }
        }

        [HttpGet]
        public async Task<IActionResult> RequestList(int page = 1)
        {
            var userId = userManager.GetUserId(User);

            try
            {
                int pageSize = 10;

                var (model, totalCount) = await friendshipService.RequestList(userId, page, pageSize);

                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                logger.LogInformation("User {UserId} accessed their pending friend requests list (Page: {Page}).", userId, page);
                return View("RequestList", model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load friend requests for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load friend requests.";
                return RedirectToAction("Index");
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeleteRequest(string friendId)
        {
            var userId = userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(friendId))
            {
                logger.LogWarning("DeleteRequest failed: Target FriendId is null for user {UserId}.", userId);
                return RedirectToAction("RequestList");
            }

            try
            {
                await friendshipService.DeleteRequest(userId, friendId);

                logger.LogInformation("User {UserId} cancelled/declined friend request related to {TargetUserId}.", userId, friendId);
                TempData["SuccessMessage"] = "Friend request cancelled.";
                return RedirectToAction("RequestList");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete friend request related to {TargetUserId} for user {UserId}.", friendId, userId);
                TempData["ErrorMessage"] = "Failed to delete the friend request.";
                return RedirectToAction("RequestList");
            }
        }
    }
}
