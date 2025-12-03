using System.Linq;
using AspNetCoreGeneratedDocument;
using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Controllers
{
    public class ProfileController : Controller
    {
        private readonly INZDbContext context;
        private readonly UserManager<User> userManager;
        public ProfileController(UserManager<User> userManager, INZDbContext dbcontext)
        {
            this.userManager = userManager;
            this.context = dbcontext;
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
                    Console.WriteLine(user.UserName);
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
            return View(model);
        }

        public async Task<IActionResult> SelectTags()
        {
            var user = await userManager.GetUserAsync(User);

            var userTagIds = await context.UserTags
                .Where(ut => ut.UserId == user.Id)
                .Select(ut => ut.TagId)
                .ToListAsync();

            var tags = await context.Tags.ToListAsync();

            var model = new SelectTagsViewModel
            {
                Tags = tags.Select(t => new TagItem
                {
                    TagId = t.Id,
                    TagName = t.Name,
                    IsSelected = userTagIds.Contains(t.Id)

                }).ToList()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SelectTags(SelectTagsViewModel model)
        {
            var user = await userManager.GetUserAsync(User);

            var selectedTagIds = model.Tags
                .Where(t => t.IsSelected)
                .Select(t => t.TagId)
                .ToList();

            var existingUserTags = await context.UserTags
                .Where(ut => ut.UserId == user.Id)
                .ToListAsync();

            context.UserTags.RemoveRange(existingUserTags);

            foreach (var tagId in selectedTagIds)
            {
                context.UserTags.Add(new UserTag
                {
                    UserId = user.Id,
                    TagId = tagId
                });
            }

            await context.SaveChangesAsync();
            return RedirectToAction("Index", "Profile");
        }

        public async Task<IActionResult> Notifications()
        {
            var user = await userManager.GetUserAsync(User);

            user = await context.Users
            .Include(u => u.ReceivedNotifications)
                .ThenInclude(n => n.Sender)
            .Include(u => u.ReceivedNotifications)
                .ThenInclude(n => n.Group)
            .FirstOrDefaultAsync(u => u.Id == user.Id);

            var model = new NotificationListViewModel
            {
                Notifications = user.ReceivedNotifications.Select(n => new NotificationViewModel
                {
                    Id = n.Id,
                    SenderUserName = n.Sender.UserName,
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
            var notification = await context.Notifications
                .Include(n => n.Sender)
                .Include(n => n.Receiver)
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification.Type == NotificationType.FriendRequest)
            {
                var Record = await context.UserFriends.FirstOrDefaultAsync(f =>
                    (f.UserId == notification.SenderId && f.FriendId == notification.ReceiverId));

                if (Record != null)
                {
                    context.UserFriends.Remove(Record);
                }
            }

            context.Notifications.Remove(notification);

            await context.SaveChangesAsync();

            return RedirectToAction("Notifications");
        }

        [HttpPost]
        public async Task<IActionResult> FriendRequestAccept(int notificationId)
        {
            var notification = await context.Notifications
                .Include(n => n.Sender)
                .Include(n => n.Receiver)
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if(notification != null)
            {
                var FirstRecord = await context.UserFriends.FirstOrDefaultAsync(f =>
                    (f.UserId == notification.SenderId && f.FriendId == notification.ReceiverId));

                if (FirstRecord != null)
                {
                    context.UserFriends.RemoveRange(FirstRecord);
                }

                context.UserFriends.AddRange(
                    new UserFriend { UserId = notification.SenderId, FriendId = notification.ReceiverId, Status = FriendshipStatus.Accepted },
                    new UserFriend { UserId = notification.ReceiverId, FriendId = notification.SenderId, Status = FriendshipStatus.Accepted }
                );

                context.Notifications.Remove(notification);

                await context.SaveChangesAsync();
            }

            return RedirectToAction("Notifications");
        }

        public async Task<IActionResult> FriendList()
        {
            var user = await userManager.GetUserAsync(User);

            var model = await context.UserFriends
                .Where(f =>
                    f.UserId == user.Id && f.Status == FriendshipStatus.Accepted)
                .Select(f => new FriendViewModel
                {
                    Id = f.Friend.Id,
                    UserName = f.Friend.UserName
                })
                .ToListAsync();

            return View("FriendList", model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFriend(string friendId)
        {
            var user = await userManager.GetUserAsync(User);

            var friendship1 = await context.UserFriends
                .FirstOrDefaultAsync(f =>
                    (f.UserId == user.Id && f.FriendId == friendId));

            var friendship2 = await context.UserFriends
                .FirstOrDefaultAsync(f =>
                    (f.FriendId == user.Id && f.UserId == friendId));

            if (friendship1 != null)
            {
                context.UserFriends.Remove(friendship1);
            }

            if (friendship2 != null)
            {
                context.UserFriends.Remove(friendship2);
            }

            await context.SaveChangesAsync();

            return RedirectToAction("FriendList");
        }

        public async Task<IActionResult> RequestList()
        {
            var user = await userManager.GetUserAsync(User);

            var model = await context.UserFriends
                .Where(f =>
                    f.UserId == user.Id && f.Status == FriendshipStatus.Pending)
                .Select(f => new FriendViewModel
                {
                    Id = f.Friend.Id,
                    UserName = f.Friend.UserName
                })
                .ToListAsync();

            return View("RequestList", model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRequest(string friendId)
        {
            var user = await userManager.GetUserAsync(User);

            var friendship = await context.UserFriends
                .FirstOrDefaultAsync(f =>
                    (f.UserId == user.Id && f.FriendId == friendId));

            if (friendship != null)
            {
                context.UserFriends.Remove(friendship);
            }

            var notification = await context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.Type == NotificationType.FriendRequest &&
                    n.SenderId == user.Id &&
                    n.ReceiverId == friendId);

            if (notification != null)
            {
                context.Notifications.Remove(notification);
            }

            await context.SaveChangesAsync();

            return RedirectToAction("RequestList");
        }

        [HttpPost]
        public async Task<IActionResult> ShowProfile(string userId)
        {
            var user = await context.Users
                .FindAsync(userId);

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

        public IActionResult EditAvatar()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EditAvatar(IFormFile AvatarFile)
        {
            var user = await userManager.GetUserAsync(User);

            if (AvatarFile != null && AvatarFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(AvatarFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await AvatarFile.CopyToAsync(stream);
                }

                user.Avatar = $"/uploads/avatars/{fileName}";
                await userManager.UpdateAsync(user);
            }

            return RedirectToAction("Index");
        }

        public IActionResult AddTag()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddTag(TagViewModel model)
        {
            Tag tag = new Tag()
            {
                Name = model.TagName
            };

            context.Tags.Add(tag);
            await context.SaveChangesAsync();

            return RedirectToAction("Index", "Profile");
        }

        /// Funkcje pomocnicze =====================================

        public async Task<IActionResult> ShowTags()
        {
            var tags = await context.Tags.ToListAsync();
            return View(tags);
        }
    }
}
