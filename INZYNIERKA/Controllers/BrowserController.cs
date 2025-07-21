using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace INZYNIERKA.Controllers
{
    public class BrowserController : Controller
    {
        private readonly INZDbContext context;
        private readonly UserManager<User> userManager;
        public BrowserController(UserManager<User> userManager, INZDbContext context)
        {
            this.context = context;
            this.userManager = userManager;
        }

        public async Task<IActionResult> SearchUsersByTags()
        {
            var tags = await context.Tags.ToListAsync();

            var viewModel = new SearchByTagsViewModel
            {
                AvailableTags = tags.Select(t => new TagCheckboxItem
                {
                    TagId = t.Id,
                    TagName = t.Name,
                    IsSelected = false
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SearchUsersByTags(SearchByTagsViewModel model)
        {
            var currentUser = await userManager.GetUserAsync(User);

            var currentUserId = currentUser.Id;

            var selectedTagIds = model.AvailableTags
                .Where(t => t.IsSelected)
                .Select(t => t.TagId)
                .ToList();

            if(selectedTagIds.Count == 0)
            {
                return View("NoSelectedTags");
            }

            var connectedUserIds = await context.UserFriends
                .Where(f =>
                    (f.UserId == currentUserId || f.FriendId == currentUserId) &&
                    f.Status == FriendshipStatus.Accepted) 
                .Select(f => f.UserId == currentUserId ? f.FriendId : f.UserId)
                .ToListAsync();

            var users = await context.Users
                .Include(u => u.UserTags)
                    .ThenInclude(ut => ut.Tag)
                .Where(u =>
                    u.Id != currentUserId &&
                    !connectedUserIds.Contains(u.Id) &&
                    selectedTagIds.All(tagId =>
                        u.UserTags.Any(ut => ut.TagId == tagId)))
                .ToListAsync();

            var random = new Random();

            var matchingUsers = users
                .OrderBy(u => random.Next())
                .Select(u => new UserViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Avatar = u.Avatar,
                    PublicDescription = u.PublicDescription,
                    Tags = u.UserTags.Select(ut => ut.Tag.Name).ToList()
                })
                .ToList();

            HttpContext.Session.SetString("MatchingUsers", JsonConvert.SerializeObject(matchingUsers));
            HttpContext.Session.SetInt32("CurrentIndex", matchingUsers.Any() ? 0 : -1);

            return RedirectToAction("ShowUser", "Browser");
        }

        [HttpGet]
        public IActionResult ShowUser(int index)
        {
            var usersJson = HttpContext.Session.GetString("MatchingUsers");

            if (string.IsNullOrEmpty(usersJson))
            {
                return RedirectToAction("SearchUsersByTags"); 
            }

            var users = JsonConvert.DeserializeObject<List<UserViewModel>>(usersJson);

            int currentIndex = HttpContext.Session.GetInt32("CurrentIndex") ?? 0;

            if (currentIndex == -1)
            {
                return View("NoSearchResults");
            }
            else
            {
                return View("SearchResults", users[currentIndex]);
            }
        }

        [HttpPost]
        public IActionResult NextUser()
        {
            var usersJson = HttpContext.Session.GetString("MatchingUsers");

            var users = JsonConvert.DeserializeObject<List<UserViewModel>>(usersJson);

            int currentIndex = HttpContext.Session.GetInt32("CurrentIndex") ?? 0;

            currentIndex++;

            if (currentIndex >= users.Count)
            {
                currentIndex = -1;
            }

            HttpContext.Session.SetInt32("CurrentIndex", currentIndex);

            return RedirectToAction("ShowUser");
        }

        [HttpPost]
        public async Task<IActionResult> SendFriendRequest(string userId)
        {
            var sender = await userManager.GetUserAsync(User);

            var receiver = await userManager.FindByIdAsync(userId);

            var existingReverseRequest = await context.UserFriends.FirstOrDefaultAsync(f =>
                f.UserId == receiver.Id &&
                f.FriendId == sender.Id &&
                f.Status == FriendshipStatus.Pending);

            if (existingReverseRequest == null)
            {

                var notification = new Notification
                {
                    SenderId = sender.Id,
                    ReceiverId = receiver.Id,
                    Type = NotificationType.FriendRequest,
                    CreationDate = DateTime.UtcNow
                };

                context.Notifications.Add(notification);

                var FriendRequestSender = new UserFriend
                {
                    UserId = sender.Id,
                    FriendId = receiver.Id,
                    Status = FriendshipStatus.Pending
                };

                context.UserFriends.Add(FriendRequestSender);

                await context.SaveChangesAsync();
            }

            var usersJson = HttpContext.Session.GetString("MatchingUsers");

            var users = JsonConvert.DeserializeObject<List<UserViewModel>>(usersJson);

            int currentIndex = HttpContext.Session.GetInt32("CurrentIndex") ?? 0;

            currentIndex++;

            if (currentIndex >= users.Count)
            {
                currentIndex = -1;
            }

            HttpContext.Session.SetInt32("CurrentIndex", currentIndex);

            return RedirectToAction("ShowUser");
        }
    }
}
