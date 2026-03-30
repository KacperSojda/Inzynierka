using System.Linq;
using INZYNIERKA.Data;
using INZYNIERKA.Models;
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

        public ProfileController(UserManager<User> userManager, 
                                 INZDbContext dbcontext, 
                                 IFriendshipService friendshipService,
                                 INotificationService notificationService,
                                 ITagService tagService,
                                 IFileService fileService)
        {
            this.userManager = userManager;
            this.context = dbcontext;
            this.friendshipService = friendshipService;
            this.notificationService = notificationService;
            this.tagService = tagService;
            this.fileService = fileService;
        }
        public async Task<IActionResult> Index()
        {
            var user = await userManager.GetUserAsync(User);

            var userTags = await context.UserTags
                .Where(ut => ut.UserId == user.Id)
                .Include(ut => ut.Tag)
                .ToListAsync();

            var model = new UserViewModel
            {
                PrivateDescription = user.PrivateDescription,
                PublicDescription = user.PublicDescription,
                UserName = user.UserName,
                Avatar = user.Avatar,
                Tags = userTags.Select(ut => ut.Tag.Name).ToList(),
            };
            return View(model);
        }

        public async Task<IActionResult> EditProfile()
        {
            var user = await userManager.GetUserAsync(User);

            var model = new UserViewModel
            {
                PrivateDescription = user.PrivateDescription,
                PublicDescription = user.PublicDescription,
                UserName = user.UserName,
                Avatar = user.Avatar,
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(UserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.GetUserAsync(User);

                if (user == null) return NotFound();

                user.Avatar = model.Avatar;
                user.PublicDescription = model.PublicDescription;
                user.PrivateDescription = model.PrivateDescription;

                var result = await userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Profile");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
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
            var model = new NotificationListViewModel
            {
                Notifications = user.ReceivedNotifications.Select(n => new NotificationViewModel
                {
                    Id = n.Id,
                    SenderUserName = n.Sender != null ? n.Sender.UserName : "System",
                    GroupName = n.Group != null ? n.Group.Name : "Error",
                    NotificationType = n.Type,
                    CreationDate = n.CreationDate
                }).OrderByDescending(n => n.CreationDate).ToList()
            };

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

        [HttpGet]
        public async Task<IActionResult> ShowProfile(string userId)
        {
            var user = await context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var userTags = await context.UserTags
                .Where(t => t.UserId == userId)
                .Select(t => t.Tag.Name)
                .ToListAsync();

            var model = new UserViewModel
            {
                Id = userId,
                Avatar = user.Avatar,
                UserName = user.UserName,
                PublicDescription = user.PublicDescription,
                PrivateDescription = "",
                Tags = userTags
            };

            return View(model);
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
