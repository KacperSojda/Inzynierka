using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace INZYNIERKA.Controllers
{
    [Authorize]
    public class BrowserController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly IMatchmakingService<User> matchmakingService;
        private readonly IFriendshipService<User> friendshipService;
        private readonly IAiMatchmakingService<User> aiMatchmakingService;
        private readonly ILogger<BrowserController> logger;

        public BrowserController(
            UserManager<User> userManager, 
            IMatchmakingService<User> matchmakingService,
            IFriendshipService<User> friendshipService,
            IAiMatchmakingService<User> aiMatchmakingService,
            ILogger<BrowserController> logger)
        {
            this.userManager = userManager;
            this.matchmakingService = matchmakingService;
            this.friendshipService = friendshipService;
            this.aiMatchmakingService = aiMatchmakingService;
            this.logger = logger;
        }

        // Matchmaking Service //

        public async Task<IActionResult> SearchUsersByTags()
        {
            var userId = userManager.GetUserId(User);
            try
            {
                var browserViewModel = await matchmakingService.Tags();
                logger.LogInformation("User {UserId} accessed the tag search browser.", userId);
                return View(browserViewModel);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Failed to load browser filters for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load browser";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SearchUsersByTags(SearchByTagsViewModel model)
        {
            var userId = userManager.GetUserId(User);
            try
            {
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
                    logger.LogInformation("Search by user {UserId} yielded 0 results.", userId);
                    return RedirectToAction("SearchUsersByTags");
                }

                logger.LogInformation("Search by user {UserId} yielded {Count} results.", userId, matchedUsersIds.Count);

                HttpContext.Session.SetString("matchingUsers", JsonConvert.SerializeObject(matchedUsersIds));
                HttpContext.Session.SetInt32("currentIndex", 0);

                return RedirectToAction("ShowUser", "Browser");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during the search process for user {UserId}.", userId);
                return RedirectToAction("SearchUsersByTags");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ShowUser()
        {
            var userId = userManager.GetUserId(User);
            try
            {
                var users = HttpContext.Session.GetString("matchingUsers");

                if (string.IsNullOrEmpty(users))
                {
                    logger.LogWarning("ShowUser failed: Search session expired or is empty for user {UserId}.", userId);
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
                    logger.LogWarning("Matched user {TargetUserId} not found in database.", targetUserId);
                    return View("NoSearchResults");
                }

                return View("SearchResults", model);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load matched user profile for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load user profile.";
                return View("NoSearchResults");
            }
        }

        [HttpPost]
        public IActionResult NextUser()
        {
            var userId = userManager.GetUserId(User);
            try
            {
                var users = HttpContext.Session.GetString("matchingUsers");

                if (string.IsNullOrEmpty(users))
                {
                    logger.LogWarning("NextUser failed: Search session expired for user {UserId}.", userId);
                    TempData["ErrorMessage"] = "Your search session has expired.";
                    return RedirectToAction("SearchUsersByTags");
                }

                var usersIds = JsonConvert.DeserializeObject<List<string>>(users);

                int currentIndex = HttpContext.Session.GetInt32("currentIndex") ?? 0;

                currentIndex++;

                if (currentIndex >= usersIds.Count)
                {
                    logger.LogInformation("User {UserId} reached the end of the search results.", userId);
                    currentIndex = -1;
                }

                HttpContext.Session.SetInt32("currentIndex", currentIndex);

                return RedirectToAction("ShowUser");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while loading the next user for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load user.";
                return RedirectToAction("SearchUsersByTags");
            }
        }

        // Friendship Service //

        [HttpPost]
        public async Task<IActionResult> SendFriendRequest(string userId)
        {
            var currentUserId = userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("User {CurrentUserId} attempted to send a friend request without specifying a target user.", currentUserId);
                TempData["ErrorMessage"] = "Could not identify the user.";
                return NextUser();
            }

            try
            {
                logger.LogInformation("User {CurrentUserId} successfully sent a friend request to {TargetUserId}.", currentUserId, userId);
                await friendshipService.SendRequest(currentUserId, userId);
                TempData["SuccessMessage"] = "Friend request sent successfully!";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send friend request from user {CurrentUserId} to {TargetUserId}.", currentUserId, userId);
                TempData["ErrorMessage"] = "Failed to send friend request.";
            }

            return NextUser();
        }

        // AI Matchmaking Service //

        [HttpGet]
        public async Task<IActionResult> SearchWithAI()
        {
            var userId = userManager.GetUserId(User);
            try
            {
                logger.LogInformation("User {UserId} initiated AI matchmaking.", userId);
                var matchingUsers = await aiMatchmakingService.AiMatches(userId);

                HttpContext.Session.SetString("matchingUsers", JsonConvert.SerializeObject(matchingUsers));
                HttpContext.Session.SetInt32("currentIndex", 0);

                logger.LogInformation("AI matchmaking for user {UserId} found {Count} results.", userId, matchingUsers.Count);

                return RedirectToAction("ShowUserWithAI");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AI matchmaking failed for user {UserId}.", userId);
                return View("NoSearchResults");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ShowUserWithAI()
        {
            var currentUserId = userManager.GetUserId(User);
            try
            {
                var users = HttpContext.Session.GetString("matchingUsers");

                if (string.IsNullOrEmpty(users))
                {
                    logger.LogWarning("ShowUserWithAI failed: AI search session expired for user {UserId}.", currentUserId);
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

                var (matchedUser, newIndex) = await aiMatchmakingService.AiNext(currentUserId, usersIds, currentIndex);

                if (matchedUser != null)
                {
                    HttpContext.Session.SetInt32("currentIndex", newIndex);
                    return View("SearchAiResults", matchedUser);
                }

                logger.LogInformation("User {UserId} reached the end of the AI search results.", currentUserId);
                HttpContext.Session.SetInt32("currentIndex", -1);
                return View("NoSearchResults");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load AI matched user profile for user {UserId}.", currentUserId);
                TempData["ErrorMessage"] = "Failed to load user profile";
                return View("NoSearchResults");
            }
        }
    }
}
