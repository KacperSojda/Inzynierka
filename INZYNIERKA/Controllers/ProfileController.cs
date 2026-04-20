using System.Linq;
using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.ViewModels;
using INZYNIERKA.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace INZYNIERKA.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly INZDbContext context;
        private readonly UserManager<User> userManager;
        private readonly IFriendshipService friendshipService;
        private readonly INotificationService notificationService;
        private readonly ITagService tagService;
        private readonly IFileService fileService;
        private readonly IProfileService profileService;

        public ProfileController(
            INZDbContext context,
            UserManager<User> userManager, 
            IFriendshipService friendshipService,
            INotificationService notificationService,
            ITagService tagService,
            IFileService fileService,
            IProfileService profileService)
        {
            this.context = context;
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

            if (model == null) return NotFound();

            return View(model);
        }

        public async Task<IActionResult> EditProfile()
        {
            var userId = userManager.GetUserId(User);
            var model = await profileService.GetUserProfileForEditAsync(userId);

            if (model == null) return NotFound();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(UserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = userManager.GetUserId(User);

            var (isSuccess, errors) = await profileService.UpdateUserProfileAsync(userId, model);

            if (isSuccess) return RedirectToAction("Index");

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ShowProfile(string userId)
        {
            var model = await profileService.GetOtherUserProfileAsync(userId);

            if (model == null) return NotFound();

            return View(model);
        }

        // Tag Service //

        public async Task<IActionResult> SelectTags()
        {
            var userId = userManager.GetUserId(User);

            var model = await tagService.GetUserTagsForSelectionAsync(userId);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SelectTags(SelectTagsViewModel model)
        {
            var userId = userManager.GetUserId(User);

            var selectedTagIds = model.Tags
                .Where(t => t.IsSelected)
                .Select(t => t.TagId)
                .ToList();

            await tagService.UpdateUserTagsAsync(userId, selectedTagIds);

            return RedirectToAction("Index", "Profile");
        }

        public IActionResult AddTag()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddTag(TagViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await tagService.AddNewTagAsync(model.TagName);

            return RedirectToAction("Index", "Profile");
        }

        public async Task<IActionResult> ShowTags()
        {
            var tags = await context.Tags.ToListAsync();
            return View(tags);
        }

        // Notification Service //

        public async Task<IActionResult> Notifications()
        {
            var userId = userManager.GetUserId(User);

            var model = await notificationService.GetNotificationsAsync(userId);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteNotification(int notificationId)
        {
            var userId = userManager.GetUserId(User);

            var success = await notificationService.DeleteNotificationAsync(userId, notificationId);

            if (!success)
            {
                return NotFound();
            }

            return RedirectToAction("Notifications");
        }

        // Friendship Service //

        [HttpPost]
        public async Task<IActionResult> FriendRequestAccept(int notificationId)
        {
            var userId = userManager.GetUserId(User);
            var result = await friendshipService.AcceptFriendRequestAsync(userId, notificationId);

            if (!result)
            {
                return NotFound();
            }

            return RedirectToAction("Notifications");
        }

        public async Task<IActionResult> FriendList()
        {
            var userId = userManager.GetUserId(User);
            var model = await friendshipService.GetFriendListAsync(userId);

            return View("FriendList", model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFriend(string friendId)
        {
            var userId = userManager.GetUserId(User);

            await friendshipService.DeleteFriendAsync(userId, friendId);

            return RedirectToAction("FriendList");
        }

        public async Task<IActionResult> RequestList()
        {
            var userId = userManager.GetUserId(User);

            var model = await friendshipService.GetRequestListAsync(userId);

            return View("RequestList", model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRequest(string friendId)
        {
            var userId = userManager.GetUserId(User);

            await friendshipService.DeleteRequestAsync(userId, friendId);

            return RedirectToAction("RequestList");
        }

        // File Service // 

        public IActionResult EditAvatar()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EditAvatar(IFormFile AvatarFile)
        {
            var user = await userManager.GetUserAsync(User);

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
    }
}
