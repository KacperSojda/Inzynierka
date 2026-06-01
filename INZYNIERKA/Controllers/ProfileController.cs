using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
        public ProfileController(
            UserManager<User> userManager, 
            IFriendshipService<User> friendshipService,
            INotificationService<User> notificationService,
            ITagService<User> tagService,
            IFileService fileService,
            IProfileService<User> profileService)
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
            var model = await profileService.Profile(userId);

            if (model == null)
            {
                return NotFound("Cannot find user profile.");
            }

            return View(model);
        }

        public async Task<IActionResult> EditProfile()
        {
            try
            {
                var userId = userManager.GetUserId(User);
                var model = await profileService.EditProfile(userId);

                if (model == null) return NotFound("Cannot find the user profile");

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
            if (model == null)
            {
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var userId = userManager.GetUserId(User);

                var (result, errorMessage) = await profileService.UpdateProfile(userId, model);

                if (result)
                {
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError("", errorMessage);

                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Server error");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ShowProfile(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var model = await profileService.OtherProfile(userId);

                if (model == null)
                {
                    return NotFound("Cannot find user profile.");
                }

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
                var model = await tagService.UserTags(userId);

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
            if (model == null)
            {
                ModelState.AddModelError("", "No data");
                return RedirectToAction("Index");
            }

            try
            {
                var userId = userManager.GetUserId(User);

                var selectedTagsIds = model.Tags
                    .Where(t => t.Selected)
                    .Select(t => t.TagId)
                    .ToList();

                await tagService.UpdateUserTags(userId, selectedTagsIds);

                return RedirectToAction("Index", "Profile");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Server error");
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
            if (model == null || !ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var (result, errorMessage) = await tagService.NewTag(model.TagName);
                if (result)
                {
                    return RedirectToAction("Index", "Profile");
                }

                ModelState.AddModelError("", errorMessage);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Server error");
                return View(model);
            }
        }

        public async Task<IActionResult> ShowTags()
        {
            try
            {
                var tags = await tagService.AllTags();
                return View(tags);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index");
            }
        }

        // Notification Service //

        public async Task<IActionResult> Notifications(int page = 1)
        {
            try
            {
                var userId = userManager.GetUserId(User);
                int pageSize = 10;

                var (model, totalCount) = await notificationService.Notifications(userId, page, pageSize);

                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

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
            if (notificationId <= 0)
            {
                return RedirectToAction("Notifications");
            }

            try
            {
                var userId = userManager.GetUserId(User);
                var success = await notificationService.DeleteNotification(userId, notificationId);

                if (!success)
                {
                    return NotFound("Cannot delete the notification.");
                }

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
            if (notificationId <= 0)
            {
                return RedirectToAction("Notifications");
            }

            try
            {
                var userId = userManager.GetUserId(User);
                var result = await friendshipService.AcceptRequest(userId, notificationId);

                if (!result)
                {
                    return NotFound("Cannot accept the friend request.");
                }

                return RedirectToAction("Notifications");
            }
            catch (Exception ex)
            {
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
            if (string.IsNullOrWhiteSpace(friendId))
            {
                return RedirectToAction("FriendList");
            }

            try
            {
                var userId = userManager.GetUserId(User);
                await friendshipService.DeleteFriend(userId, friendId);

                return RedirectToAction("FriendList");
            }
            catch (Exception ex)
            {
                return RedirectToAction("FriendList");
            }
        }

        [HttpGet]
        public async Task<IActionResult> RequestList(int page = 1)
        {
            try
            {
                var userId = userManager.GetUserId(User);
                int pageSize = 10;

                var (model, totalCount) = await friendshipService.RequestList(userId, page, pageSize);

                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

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
            if (string.IsNullOrWhiteSpace(friendId))
            {
                return RedirectToAction("RequestList");
            }

            try
            {
                var userId = userManager.GetUserId(User);

                await friendshipService.DeleteRequest(userId, friendId);

                return RedirectToAction("RequestList");
            }
            catch (Exception ex)
            {
                return RedirectToAction("RequestList");
            }
        }

        // File Service // 

        [HttpGet]
        public async Task<IActionResult> EditMedia()
        {
            try
            {
                var userId = userManager.GetUserId(User);
                var model = await profileService.Profile(userId);

                if (model == null)
                {
                    return NotFound("Nie znaleziono profilu użytkownika.");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditAvatar(IFormFile AvatarFile)
        {
            if (AvatarFile == null || AvatarFile.Length == 0)
            {
                return RedirectToAction("EditMedia");
            }

            try
            {
                var (result, avatar) = await fileService.UploadAvatar(AvatarFile);

                if (!result)
                {
                    return RedirectToAction("EditMedia");
                }

                var userId = userManager.GetUserId(User);

                result = await profileService.UpdateAvatar(userId, avatar);

                return RedirectToAction("EditMedia");
            }
            catch (Exception ex)
            {
                return RedirectToAction("EditMedia");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditCover(IFormFile CoverFile)
        {
            if (CoverFile == null || CoverFile.Length == 0)
            {
                return RedirectToAction("EditMedia");
            }

            try
            {
                var (result, cover) = await fileService.UploadAvatar(CoverFile);

                if (!result)
                {
                    return RedirectToAction("EditMedia");
                }

                var userId = userManager.GetUserId(User);
                var success = await profileService.UpdateCover(userId, cover);

                return RedirectToAction("EditMedia");
            }
            catch (Exception ex)
            {
                return RedirectToAction("EditMedia");
            }
        }
    }
}
