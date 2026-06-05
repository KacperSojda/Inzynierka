using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;

namespace INZYNIERKA.Controllers
{
    [Authorize]
    public class BrowserController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly IMatchmakingService<User> matchmakingService;
        private readonly IFriendshipService<User> friendshipService;
        private readonly IAiMatchmakingService<User> aiMatchmakingService;
        public BrowserController(
            UserManager<User> userManager, 
            IMatchmakingService<User> matchmakingService,
            IFriendshipService<User> friendshipService,
            IAiMatchmakingService<User> aiMatchmakingService)
        {
            this.userManager = userManager;
            this.matchmakingService = matchmakingService;
            this.friendshipService = friendshipService;
            this.aiMatchmakingService = aiMatchmakingService;
        }

        // Matchmaking Service //

        public async Task<IActionResult> SearchUsersByTags()
        {
            try
            {
                var browserViewModel = await matchmakingService.Tags();

                return View(browserViewModel);
            }
            catch(Exception)
            {
                TempData["ErrorMessage"] = "Failed to load browser";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SearchUsersByTags(SearchByTagsViewModel model)
        {
            try
            {
                var userId = userManager.GetUserId(User);

                var tagIds = model.AvailableTags
                    .Where(t => t.Selected)
                    .Select(t => t.TagId)
                    .ToList();

                var matchedUsersIds = await matchmakingService.MatchingUsersIds(
                    userId,
                    tagIds,
                    model.SearchName,
                    model.SearchCity,
                    model.SearchCountry
                );

                if (matchedUsersIds.Count == 0)
                {
                    return RedirectToAction("SearchUsersByTags");
                }

                HttpContext.Session.SetString("matchingUsers", JsonConvert.SerializeObject(matchedUsersIds));

                HttpContext.Session.SetInt32("currentIndex", 0);

                return RedirectToAction("ShowUser", "Browser");
            }
            catch (Exception)
            {
                return RedirectToAction("SearchUsersByTags");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ShowUser()
        {
            try
            {
                var users = HttpContext.Session.GetString("matchingUsers");

                if (string.IsNullOrEmpty(users))
                {
                    TempData["ErrorMessage"] = "Your search session has expired";
                    return RedirectToAction("SearchUsersByTags");
                }

                var usersIds = JsonConvert.DeserializeObject<List<string>>(users);

                int currentIndex = HttpContext.Session.GetInt32("currentIndex") ?? 0;

                if(currentIndex == -1 || currentIndex >= usersIds.Count)
                {
                    return View("NoSearchResults");
                }

                var targetUserId = usersIds[currentIndex];

                var model = await matchmakingService.MatchedUser(targetUserId);

                if (model == null)
                {
                    return View("NoSearchResults");
                }

                return View("SearchResults", model);

            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to load user profile.";
                return View("NoSearchResults");
            }
        }

        [HttpPost]
        public IActionResult NextUser()
        {
            try
            {
                var users = HttpContext.Session.GetString("matchingUsers");

                if (string.IsNullOrEmpty(users))
                {
                    TempData["ErrorMessage"] = "Your search session has expired.";
                    return RedirectToAction("SearchUsersByTags");
                }

                var usersIds = JsonConvert.DeserializeObject<List<string>>(users);

                int currentIndex = HttpContext.Session.GetInt32("currentIndex") ?? 0;

                currentIndex++;

                if (currentIndex >= usersIds.Count)
                {
                    currentIndex = -1;
                }

                HttpContext.Session.SetInt32("currentIndex", currentIndex);

                return RedirectToAction("ShowUser");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to load user.";
                return RedirectToAction("SearchUsersByTags");
            }
        }

        // Friendship Service //

        [HttpPost]
        public async Task<IActionResult> SendFriendRequest(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Could not identify the user.";
                return NextUser();
            }

            try
            {
                var currentUserId = userManager.GetUserId(User);
                await friendshipService.SendRequest(currentUserId, userId);
                TempData["SuccessMessage"] = "Friend request sent successfully!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to send friend request.";
            }

            return NextUser();
        }

        // AI Matchmaking Service //

        [HttpGet]
        public async Task<IActionResult> SearchWithAI()
        {
            try
            {
                var userId = userManager.GetUserId(User);
                var matchingUsers = await aiMatchmakingService.AiMatches(userId);

                HttpContext.Session.SetString("matchingUsers", JsonConvert.SerializeObject(matchingUsers));
                HttpContext.Session.SetInt32("currentIndex", 0);

                return RedirectToAction("ShowUserWithAI");
            }
            catch (Exception)
            {
                return View("NoSearchResults");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ShowUserWithAI()
        {
            try
            {
                var users = HttpContext.Session.GetString("matchingUsers");

                if (string.IsNullOrEmpty(users))
                {
                    TempData["ErrorMessage"] = "Your search session has expired.";
                    return RedirectToAction("SearchUsersByTags");
                }

                var usersIds = JsonConvert.DeserializeObject<List<string>>(users);

                if (usersIds == null || usersIds.Count == 0)
                {
                    return View("NoSearchResults");
                }

                int currentIndex = HttpContext.Session.GetInt32("currentIndex") ?? 0;

                if (currentIndex == -1 || currentIndex >= usersIds.Count)
                {
                    return View("NoSearchResults");
                }

                var currentUserId = userManager.GetUserId(User);

                var (matchedUser, newIndex) = await aiMatchmakingService.AiNext(currentUserId, usersIds, currentIndex);

                if (matchedUser != null)
                {
                    HttpContext.Session.SetInt32("currentIndex", newIndex);
                    return View("SearchAiResults", matchedUser);
                }

                HttpContext.Session.SetInt32("currentIndex", -1);
                return View("NoSearchResults");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to load user profile";
                return View("NoSearchResults");
            }
        }
    }
}
