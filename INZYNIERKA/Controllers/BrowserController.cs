using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.Services;
using INZYNIERKA.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace INZYNIERKA.Controllers
{
    [Authorize]
    public class BrowserController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly IMatchmakingService matchmakingService;
        private readonly IFriendshipService friendshipService;
        private readonly IAiMatchmakingService aiMatchmakingService;
        public BrowserController(
            UserManager<User> userManager, 
            IMatchmakingService matchmakingService,
            IFriendshipService friendshipService,
            IAiMatchmakingService aiMatchmakingService)
        {
            this.userManager = userManager;
            this.matchmakingService = matchmakingService;
            this.friendshipService = friendshipService;
            this.aiMatchmakingService = aiMatchmakingService;
        }

        // Matchmaking Service //

        public async Task<IActionResult> SearchUsersByTags()
        {
            var viewModel = await matchmakingService.GetTagsForSearchAsync();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SearchUsersByTags(SearchByTagsViewModel model)
        {
            var userId = userManager.GetUserId(User);

            var selectedTagIds = model.AvailableTags
                .Where(t => t.IsSelected)
                .Select(t => t.TagId)
                .ToList();

            if (selectedTagIds.Count == 0) return View("NoSelectedTags");

            var matchedUserIds = await matchmakingService.GetMatchingUserIdsByTagsAsync(userId, selectedTagIds);

            HttpContext.Session.SetString("MatchingUsers", JsonConvert.SerializeObject(matchedUserIds));
            HttpContext.Session.SetInt32("CurrentIndex", matchedUserIds.Any() ? 0 : -1);

            return RedirectToAction("ShowUser", "Browser");
        }

        [HttpGet]
        public async Task<IActionResult> ShowUser()
        {
            var usersJson = HttpContext.Session.GetString("MatchingUsers");

            if (string.IsNullOrEmpty(usersJson)) return RedirectToAction("SearchUsersByTags");

            var usersIds = JsonConvert.DeserializeObject<List<string>>(usersJson);

            int currentIndex = HttpContext.Session.GetInt32("CurrentIndex") ?? 0;

            if (currentIndex == -1 || currentIndex >= usersIds.Count) return View("NoSearchResults");

            var targetUserId = usersIds[currentIndex];

            var model = await matchmakingService.GetUserForBrowserAsync(targetUserId);

            if (model == null) return View("NoSearchResults");

            return View("SearchResults", model);
        }

        [HttpPost]
        public IActionResult NextUser()
        {
            var usersJson = HttpContext.Session.GetString("MatchingUsers");

            if (string.IsNullOrEmpty(usersJson)) return RedirectToAction("SearchUsersByTags");

            var users = JsonConvert.DeserializeObject<List<string>>(usersJson);

            int currentIndex = HttpContext.Session.GetInt32("CurrentIndex") ?? 0;

            currentIndex++;

            if (currentIndex >= users.Count) currentIndex = -1;

            HttpContext.Session.SetInt32("CurrentIndex", currentIndex);

            return RedirectToAction("ShowUser");
        }

        // Friendship Service //

        [HttpPost]
        public async Task<IActionResult> SendFriendRequest(string userId)
        {
            var currentUserId = userManager.GetUserId(User);

            await friendshipService.SendFriendRequestAsync(currentUserId, userId);

            return NextUser();
        }

        // AI Matchmaking Service //

        [HttpGet]
        public async Task<IActionResult> SearchWithAI()
        {
            var userId = userManager.GetUserId(User);

            var matchingUsers = await aiMatchmakingService.GetPotentialMatchesForAiAsync(userId);

            HttpContext.Session.SetString("MatchingUsers", JsonConvert.SerializeObject(matchingUsers));
            HttpContext.Session.SetInt32("CurrentIndex", matchingUsers.Any() ? 0 : -1);

            return RedirectToAction("ShowUserWithAI");
        }

        [HttpGet]
        public async Task<IActionResult> ShowUserWithAI()
        {
            var usersJson = HttpContext.Session.GetString("MatchingUsers");

            if (string.IsNullOrEmpty(usersJson)) return RedirectToAction("SearchUsersByTags");

            var users = JsonConvert.DeserializeObject<List<string>>(usersJson);
            int currentIndex = HttpContext.Session.GetInt32("CurrentIndex") ?? 0;

            if (currentIndex == -1 || currentIndex >= users.Count) return View("NoSearchResults");

            var currentUserId = userManager.GetUserId(User);

            var (matchedUser, newIndex) = await aiMatchmakingService.FindNextAiMatchAsync(currentUserId, users, currentIndex);

            if (matchedUser != null)
            {
                HttpContext.Session.SetInt32("CurrentIndex", newIndex);
                return View("SearchAiResults", matchedUser);
            }

            HttpContext.Session.SetInt32("CurrentIndex", -1);
            return View("NoSearchResults");
        }
    }
}
