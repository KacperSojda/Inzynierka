using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.Services;
using INZYNIERKA.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace INZYNIERKA.Controllers
{
    [Authorize]
    public class BrowserController : Controller
    {
        private readonly INZDbContext context;
        private readonly UserManager<User> userManager;
        private readonly GeminiService geminiService;
        public BrowserController(UserManager<User> userManager, INZDbContext context, GeminiService geminiService)
        {
            this.context = context;
            this.userManager = userManager;
            this.geminiService = geminiService;
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

            if (selectedTagIds.Count == 0)
            {
                return View("NoSelectedTags");
            }

            var connectedUserIds = await context.UserFriends
                .Where(f =>
                    (f.UserId == currentUserId || f.FriendId == currentUserId))
                .Select(f => f.UserId == currentUserId ? f.FriendId : f.UserId)
                .ToListAsync();

            var matchingUserIds = await context.Users
                .Where(u =>
                    u.Id != currentUserId &&
                    !connectedUserIds.Contains(u.Id) &&
                    selectedTagIds.All(tagId =>
                        u.UserTags.Any(ut => ut.TagId == tagId)))
                .Select(u => u.Id)
                .ToListAsync();

            var random = new Random();

            matchingUserIds = matchingUserIds.OrderBy(id => random.Next()).ToList();

            HttpContext.Session.SetString("MatchingUsers", JsonConvert.SerializeObject(matchingUserIds));
            HttpContext.Session.SetInt32("CurrentIndex", matchingUserIds.Any() ? 0 : -1);

            return RedirectToAction("ShowUser", "Browser");
        }

        [HttpGet]
        public async Task<IActionResult> ShowUser()
        {
            var usersJson = HttpContext.Session.GetString("MatchingUsers");

            if (string.IsNullOrEmpty(usersJson))
            {
                return RedirectToAction("SearchUsersByTags");
            }

            var userIds = JsonConvert.DeserializeObject<List<string>>(usersJson);

            int currentIndex = HttpContext.Session.GetInt32("CurrentIndex") ?? 0;

            if (currentIndex == -1 || currentIndex >= userIds.Count)
            {
                return View("NoSearchResults");
            }

            var targetUserId = userIds[currentIndex];

            var user = await context.Users
                .Include(u => u.UserTags)
                    .ThenInclude(ut => ut.Tag)
                .FirstOrDefaultAsync(u => u.Id == targetUserId);

            if (user == null)
            {
                return View("NoSearchResults");
            }

            var model = new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Avatar = user.Avatar,
                PublicDescription = user.PublicDescription,
                Tags = user.UserTags.Select(ut => ut.Tag.Name).ToList()
            };

            return View("SearchResults", model);
        }

        [HttpPost]
        public IActionResult NextUser()
        {
            var usersJson = HttpContext.Session.GetString("MatchingUsers");

            if (string.IsNullOrEmpty(usersJson))
            {
                return RedirectToAction("SearchUsersByTags");
            }

            var users = JsonConvert.DeserializeObject<List<string>>(usersJson);

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

            return NextUser();
        }

        [HttpGet]
        public async Task<IActionResult> SearchWithAI()
        {
            var currentUserId = userManager.GetUserId(User);

            var user = await context.Users
                .Include(u => u.UserTags)
                    .ThenInclude(ut => ut.Tag)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (user == null)
                return NotFound();

            var connectedUserIds = await context.UserFriends
                .Where(f =>
                    (f.UserId == currentUserId || f.FriendId == currentUserId))
                .Select(f => f.UserId == currentUserId ? f.FriendId : f.UserId)
                .ToListAsync();

            var matchingUsers = await context.Users
                .Where(u =>
                    u.Id != currentUserId &&
                    !connectedUserIds.Contains(u.Id))
                .OrderBy(u => Guid.NewGuid())
                .Select(u => u.Id)
                .ToListAsync();

            HttpContext.Session.SetString("MatchingUsers", JsonConvert.SerializeObject(matchingUsers));
            HttpContext.Session.SetInt32("CurrentIndex", matchingUsers.Any() ? 0 : -1);

            return RedirectToAction("ShowUserWithAI");
        }

        [HttpGet]
        public async Task<IActionResult> ShowUserWithAI()
        {
            var usersJson = HttpContext.Session.GetString("MatchingUsers");

            if (string.IsNullOrEmpty(usersJson))
            {
                return RedirectToAction("SearchUsersByTags");
            }

            var users = JsonConvert.DeserializeObject<List<string>>(usersJson);

            int currentIndex = HttpContext.Session.GetInt32("CurrentIndex") ?? 0;

            if (currentIndex == -1 || currentIndex >= users.Count)
            {
                return View("NoSearchResults");
            }
            else
            {
                var userId = userManager.GetUserId(User);

                var user = await context.Users
                    .Include(u => u.UserTags)
                        .ThenInclude(ut => ut.Tag)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return NotFound();

                var tags = user.UserTags.Select(ut => ut.Tag.Name).ToList();

                var combinedString = $"First Description: {user.PublicDescription} {user.PrivateDescription} Hobby: {string.Join(", ", tags)}";

                while (currentIndex < users.Count)
                {

                    var currentUser = users[currentIndex];

                    currentIndex++;

                    var dbUser = await context.Users
                        .Include(u => u.UserTags)
                            .ThenInclude(ut => ut.Tag)
                        .FirstOrDefaultAsync(u => u.Id == currentUser);

                    if (dbUser == null)
                    {
                        continue;
                    }

                    var friendTags = dbUser.UserTags.Select(ut => ut.Tag.Name);

                    var friendCombinedString = $"Second Description: {dbUser.PublicDescription} Hobby: {string.Join(", ", friendTags)}";

                    var promptString = combinedString + " " + friendCombinedString;

                    var geminiAns = await geminiService.AskAsync(promptString, "You will receive two descriptions of 2 people. Determine if they have anything in common based on the information given. do not add any additional descriptions or characters. Respond with only one word: YES if they share any common characteristics, otherwise NO.");

                    if (geminiAns.Trim().ToUpper().Contains("YES"))
                    {
                        var model = new UserViewModel
                        {
                            Id = dbUser.Id,
                            UserName = dbUser.UserName,
                            Avatar = dbUser.Avatar,
                            PublicDescription = dbUser.PublicDescription,
                            Tags = dbUser.UserTags.Select(ut => ut.Tag.Name).ToList()
                        };

                        HttpContext.Session.SetInt32("CurrentIndex", currentIndex);

                        return View("SearchAiResults", model);
                    }
                    await Task.Delay(15);
                }
                return View("NoSearchResults");
            }
        }
    }
}
