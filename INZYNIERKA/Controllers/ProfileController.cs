using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.ViewModels;
using INZYNIERKA.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace INZYNIERKA.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly IFriendshipService friendshipService;
        private readonly INotificationService notificationService;
        private readonly ITagService tagService;
        private readonly IFileService fileService;
        private readonly IProfileService profileService;

        public ProfileController(
            UserManager<User> userManager, 
            IFriendshipService friendshipService,
            INotificationService notificationService,
            ITagService tagService,
            IFileService fileService,
            IProfileService profileService)
        {
            this.userManager = userManager;
            this.friendshipService = friendshipService;
            this.notificationService = notificationService;
            this.tagService = tagService;
            this.fileService = fileService;
            this.profileService = profileService;
        }

        // Profile Service //
        public async Task<IActionResult> Index()
        {
            var userId = userManager.GetUserId(User);
            var model = await profileService.GetUserProfileAsync(userId);

            if (model == null) return NotFound("Cannot find the user profile.");

            return View(model);
        }

        public async Task<IActionResult> EditProfile()
        {
            try
            {
                var userId = userManager.GetUserId(User);
                var model = await profileService.GetUserProfileForEditAsync(userId);

                if (model == null) return NotFound("Cannot find the user profile for editing.");

                return View(model);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index");
            }

        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(UserViewModel model)
        {
            if (model == null) return RedirectToAction("Index");
            if (!ModelState.IsValid) return View(model);

            try
            {
                var userId = userManager.GetUserId(User);
                var (isSuccess, errors) = await profileService.UpdateUserProfileAsync(userId, model);

                if (isSuccess) return RedirectToAction("Index");

                ModelState.AddModelError("", "Cannot update the user profile.");
                return View(model);
            }
            catch (Exception ex)
            {
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ShowProfile(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return RedirectToAction("Index", "Home");

            try
            {
                var model = await profileService.GetOtherUserProfileAsync(userId);

                if (model == null) return NotFound("Cannot find the user profile.");

                return View(model);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Home");
            }
        }

        // Tag Service //

        public async Task<IActionResult> SelectTags()
        {
            try
            {
                var userId = userManager.GetUserId(User);
                var model = await tagService.GetUserTagsForSelectionAsync(userId);

                return View(model);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SelectTags(SelectTagsViewModel model)
        {
            if (model == null) return RedirectToAction("Index");

            try
            {
                var userId = userManager.GetUserId(User);

                var selectedTagIds = model.Tags
                    .Where(t => t.IsSelected)
                    .Select(t => t.TagId)
                    .ToList();

                await tagService.UpdateUserTagsAsync(userId, selectedTagIds);

                return RedirectToAction("Index", "Profile");
            }
            catch (Exception ex)
            {
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
            if (model == null || !ModelState.IsValid) return View(model);

            try
            {
                await tagService.AddNewTagAsync(model.TagName);
                return RedirectToAction("Index", "Profile");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Cannot add the new tag.");
                return View(model);
            }
        }

        public async Task<IActionResult> ShowTags()
        {
            try
            {
                var tags = await tagService.GetAllTagsAsync();
                return View(tags);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index");
            }
        }

        // Notification Service //

        public async Task<IActionResult> Notifications()
        {
            try
            {
                var userId = userManager.GetUserId(User);
                var model = await notificationService.GetNotificationsAsync(userId);

                return View(model);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteNotification(int notificationId)
        {
            if (notificationId <= 0) return RedirectToAction("Notifications");

            try
            {
                var userId = userManager.GetUserId(User);
                var success = await notificationService.DeleteNotificationAsync(userId, notificationId);

                if (!success) return NotFound("Cannot delete the notification.");

                return RedirectToAction("Notifications");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Notifications");
            }
        }

        // Friendship Service //

        [HttpPost]
        public async Task<IActionResult> FriendRequestAccept(int notificationId)
        {
            if (notificationId <= 0) return RedirectToAction("Notifications");

            try
            {
                var userId = userManager.GetUserId(User);
                var result = await friendshipService.AcceptFriendRequestAsync(userId, notificationId);

                if (!result) return NotFound("Cannot accept the friend request.");

                return RedirectToAction("Notifications");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Notifications");
            }
        }

        public async Task<IActionResult> FriendList()
        {
            try
            {
                var userId = userManager.GetUserId(User);
                var model = await friendshipService.GetFriendListAsync(userId);

                return View("FriendList", model);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFriend(string friendId)
        {
            if (string.IsNullOrWhiteSpace(friendId)) return RedirectToAction("FriendList");

            try
            {
                var userId = userManager.GetUserId(User);
                await friendshipService.DeleteFriendAsync(userId, friendId);

                return RedirectToAction("FriendList");
            }
            catch (Exception ex)
            {
                return RedirectToAction("FriendList");
            }
        }

        public async Task<IActionResult> RequestList()
        {
            try
            {
                var userId = userManager.GetUserId(User);
                var model = await friendshipService.GetRequestListAsync(userId);

                return View("RequestList", model);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index");
            }

        }

        [HttpPost]
        public async Task<IActionResult> DeleteRequest(string friendId)
        {
            if (string.IsNullOrWhiteSpace(friendId)) return RedirectToAction("RequestList");
            try
            {
                var userId = userManager.GetUserId(User);

                await friendshipService.DeleteRequestAsync(userId, friendId);

                return RedirectToAction("RequestList");
            }
            catch (Exception ex)
            {
                return RedirectToAction("RequestList");
            }
        }

        // File Service // 

        public IActionResult EditAvatar()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EditAvatar(IFormFile AvatarFile)
        {
            if (AvatarFile == null || AvatarFile.Length == 0)
            {
                ModelState.AddModelError("", "Choose a file before saving.");
                return View();
            }

            try
            {
                var user = await userManager.GetUserAsync(User);
                if (user == null) return NotFound("User not found.");

                var uploadResult = await fileService.UploadAvatarAsync(AvatarFile);

                if (!uploadResult.IsSuccess)
                {
                    ModelState.AddModelError("", uploadResult.Result);
                    return View();
                }

                user.Avatar = uploadResult.Result;
                await userManager.UpdateAsync(user);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Cannot upload the avatar.");
                return View();
            }
        }
    }
}
