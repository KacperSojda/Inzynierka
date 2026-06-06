using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace INZYNIERKA.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly IFriendshipService<User> friendshipService;
        private readonly INotificationService<User> notificationService;
        private readonly ITagService<User> tagService;
        private readonly IFileService fileService;
        private readonly IProfileService<User> profileService;
        private readonly ILogger<ProfileController> logger;

        public ProfileController(
            UserManager<User> userManager, 
            IFriendshipService<User> friendshipService,
            INotificationService<User> notificationService,
            ITagService<User> tagService,
            IFileService fileService,
            IProfileService<User> profileService,
            ILogger<ProfileController> logger)
        {
            this.userManager = userManager;
            this.friendshipService = friendshipService;
            this.notificationService = notificationService;
            this.tagService = tagService;
            this.fileService = fileService;
            this.profileService = profileService;
            this.logger = logger;
        }

        // Profile Service //
        public async Task<IActionResult> Index()
        {
            var userId = userManager.GetUserId(User);

            try
            {
                var model = await profileService.Profile(userId);

                if (model == null)
                {
                    logger.LogWarning("Profile Index failed: User profile not found for {UserId}.", userId);
                    TempData["ErrorMessage"] = "Cannot find your user profile.";
                    return RedirectToAction("Index", "Home");
                }

                logger.LogInformation("User {UserId} viewed their profile.", userId);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load profile data for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load profile data.";
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<IActionResult> EditProfile()
        {
            var userId = userManager.GetUserId(User);

            try
            {
                var model = await profileService.EditProfile(userId);

                if (model == null)
                {
                    logger.LogWarning("EditProfile failed: User profile not found for {UserId}.", userId);
                    TempData["ErrorMessage"] = "Cannot find your user profile.";
                    return RedirectToAction("Index", "Home");
                }

                logger.LogInformation("User {UserId} accessed profile edit page.", userId);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load edit profile form for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load the edit form.";
                return RedirectToAction("Index", "Home");
            }

        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(UserViewModel model)
        {
            var userId = userManager.GetUserId(User);

            if (model == null)
            {
                logger.LogWarning("EditProfile (POST) failed: Model is null for user {UserId}.", userId);
                return RedirectToAction("Index, Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var (result, errorMessage) = await profileService.UpdateProfile(userId, model);

                if (result)
                {
                    logger.LogInformation("User {UserId} successfully updated their profile.", userId);
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToAction("Index");
                }

                logger.LogWarning("User {UserId} profile update failed. Reason: {ErrorMessage}", userId, errorMessage);
                ModelState.AddModelError("", "Update failed.");
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server error while updating profile for user {UserId}.", userId);
                ModelState.AddModelError("", "Server error");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ShowProfile(string userId)
        {
            var currentUserId = userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                logger.LogWarning("ShowProfile failed: userId parameter is null or empty. Current user: {CurrentUserId}.", currentUserId);
                TempData["ErrorMessage"] = "Cannot identify user";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var model = await profileService.OtherProfile(userId);

                if (model == null)
                {
                    logger.LogWarning("ShowProfile failed: Profile {TargetUserId} not found (Requested by: {CurrentUserId}).", userId, currentUserId);
                    TempData["ErrorMessage"] = "User profile not found.";
                    return NotFound("Cannot find user profile.");
                }

                logger.LogInformation("User {CurrentUserId} viewed profile of user {TargetUserId}.", currentUserId, userId);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load profile for user {TargetUserId} (Requested by: {CurrentUserId}).", userId, currentUserId);
                TempData["ErrorMessage"] = "Failed to load the user's profile.";
                return RedirectToAction("Index", "Home");
            }
        }

        // Tag Service //

        public async Task<IActionResult> SelectTags()
        {
            var userId = userManager.GetUserId(User);

            try
            {
                var model = await tagService.UserTags(userId);
                logger.LogInformation("User {UserId} accessed their tags selection page.", userId);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load tags for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load tags.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SelectTags(SelectTagsViewModel model)
        {
            var userId = userManager.GetUserId(User);

            if (model == null)
            {
                logger.LogWarning("SelectTags (POST) failed: Model is null for user {UserId}.", userId);
                TempData["ErrorMessage"] = "No tags data provided.";
                return RedirectToAction("Index");
            }

            try
            {
                var selectedTagsIds = model.Tags
                    .Where(t => t.Selected)
                    .Select(t => t.TagId)
                    .ToList();

                await tagService.UpdateUserTags(userId, selectedTagsIds);
                logger.LogInformation("User {UserId} updated their tags successfully.", userId);

                TempData["SuccessMessage"] = "Your tags have been updated!";
                return RedirectToAction("Index", "Profile");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server error while updating tags for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Server error while updating tags.";
                return RedirectToAction("Index");
            }
        }

        public IActionResult AddTag()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddTag(TagViewModel model)
        {
            var userId = userManager.GetUserId(User);

            if (model == null || !ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var (result, errorMessage) = await tagService.NewTag(model.TagName);
                if (result)
                {
                    logger.LogInformation("User {UserId} added new tag '{TagName}' successfully.", userId, model.TagName);
                    TempData["SuccessMessage"] = "Tag added successfully";
                    return RedirectToAction("Index", "Profile");
                }

                logger.LogWarning("User {UserId} failed to create tag '{TagName}'. Reason: {ErrorMessage}", userId, model.TagName, errorMessage);
                ModelState.AddModelError("", "Failed to add tag.");
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server error while adding tag for user {UserId}.", userId);
                ModelState.AddModelError("", "Server error");
                return View(model);
            }
        }

        public async Task<IActionResult> ShowTags()
        {
            var userId = userManager.GetUserId(User);
            try
            {
                var tags = await tagService.AllTags();
                logger.LogInformation("User {UserId} requested the list of all tags.", userId);
                return View(tags);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load tags list for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load tags list.";
                return RedirectToAction("Index");
            }
        }

        // Notification Service //

        public async Task<IActionResult> Notifications(int page = 1)
        {
            var userId = userManager.GetUserId(User);

            try
            {
                int pageSize = 10;

                var (model, totalCount) = await notificationService.Notifications(userId, page, pageSize);

                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                logger.LogInformation("User {UserId} accessed notifications page {Page}.", userId, page);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load notifications for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load notifications.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteNotification(int notificationId)
        {
            var userId = userManager.GetUserId(User);

            if (notificationId <= 0)
            {
                logger.LogWarning("DeleteNotification failed: Invalid NotificationId {NotificationId} for user {UserId}.", notificationId, userId);
                TempData["ErrorMessage"] = "Wrong notification Id";
                return RedirectToAction("Notifications");
            }

            try
            {
                var success = await notificationService.DeleteNotification(userId, notificationId);

                if (!success)
                {
                    logger.LogWarning("DeleteNotification failed: Could not delete notification {NotificationId} for user {UserId}.", notificationId, userId);
                    TempData["ErrorMessage"] = "Cannot delete the notification.";
                }
                else
                {
                    logger.LogInformation("User {UserId} deleted notification {NotificationId} successfully.", userId, notificationId);
                    TempData["SuccessMessage"] = "Notification deleted.";
                }

                return RedirectToAction("Notifications");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server error while deleting notification {NotificationId} for user {UserId}.", notificationId, userId);
                TempData["ErrorMessage"] = "Server error";
                return RedirectToAction("Notifications");
            }
        }

        // Friendship Service //

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

        // File Service // 

        [HttpGet]
        public async Task<IActionResult> EditMedia()
        {
            var userId = userManager.GetUserId(User);

            try
            {
                var model = await profileService.Profile(userId);

                if (model == null)
                {
                    logger.LogWarning("EditMedia failed: Profile not found for user {UserId}.", userId);
                    TempData["ErrorMessage"] = "User profile not found.";
                    return RedirectToAction("Index");
                }

                logger.LogInformation("User {UserId} accessed media editor.", userId);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load media editor for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load media editor.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditAvatar(IFormFile AvatarFile)
        {
            var userId = userManager.GetUserId(User);

            if (AvatarFile == null || AvatarFile.Length == 0)
            {
                logger.LogWarning("EditAvatar failed: No file uploaded by user {UserId}.", userId);
                TempData["ErrorMessage"] = "unvalid image file.";
                return RedirectToAction("EditMedia");
            }

            try
            {
                var (result, avatar) = await fileService.UploadAvatar(AvatarFile);

                if (!result)
                {
                    logger.LogWarning("EditAvatar failed: File upload failed for user {UserId}.", userId);
                    TempData["ErrorMessage"] = "Failed to upload the image.";
                    return RedirectToAction("EditMedia");
                }

                result = await profileService.UpdateAvatar(userId, avatar);

                logger.LogInformation("User {UserId} successfully updated their avatar.", userId);
                TempData["SuccessMessage"] = "Profile picture updated successfully!";
                return RedirectToAction("EditMedia");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server error while saving avatar for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Server error while saving the avatar.";
                return RedirectToAction("EditMedia");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditCover(IFormFile CoverFile)
        {
            var userId = userManager.GetUserId(User);

            if (CoverFile == null || CoverFile.Length == 0)
            {
                logger.LogWarning("EditCover failed: No file uploaded by user {UserId}.", userId);
                TempData["ErrorMessage"] = "unvalid image file.";
                return RedirectToAction("EditMedia");
            }

            try
            {
                var (result, cover) = await fileService.UploadAvatar(CoverFile);

                if (!result)
                {
                    logger.LogWarning("EditCover failed: File upload failed for user {UserId}.", userId);
                    TempData["ErrorMessage"] = "Failed to upload the image";
                    return RedirectToAction("EditMedia");
                }

                var success = await profileService.UpdateCover(userId, cover);
                logger.LogInformation("User {UserId} successfully updated their cover photo.", userId);

                TempData["SuccessMessage"] = "Cover photo updated successfully.";
                return RedirectToAction("EditMedia");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server error while saving cover photo for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Server error";
                return RedirectToAction("EditMedia");
            }
        }
    }
}
