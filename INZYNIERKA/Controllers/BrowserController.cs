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
        private readonly UserManager<User> _userManager;
        private readonly IMatchmakingService<User> _matchmakingService;
        private readonly IFriendshipService<User> _friendshipService;
        private readonly IAiMatchmakingService<User> _aiMatchmakingService;
        private readonly ILogger<BrowserController> _logger;
        public BrowserController(
            UserManager<User> userManager, 
            IMatchmakingService<User> matchmakingService,
            IFriendshipService<User> friendshipService,
            IAiMatchmakingService<User> aiMatchmakingService,
            ILogger<BrowserController> logger)
        {
            this._userManager = userManager;
            this._matchmakingService = matchmakingService;
            this._friendshipService = friendshipService;
            this._aiMatchmakingService = aiMatchmakingService;
            this._logger = logger;
        }

        // Matchmaking Service //

        [HttpGet]
        public async Task<IActionResult> SearchUsersByTags()
        {
            var userId = _userManager.GetUserId(User);
            try
            {
                var model = await _matchmakingService.Tags();
                _logger.LogInformation("User {UserId} accessed the tag search browser.", userId);
                return View(model);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to load browser filters for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load browser";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SearchUsersByTags(SearchByTagsViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            try
            {
                var tagIds = model.AvailableTags
                    .Where(t => t.Selected)
                    .Select(t => t.TagId)
                    .ToList();

                var matchedUsersIds = await _matchmakingService.MatchingUsersIds(
                    userId,
                    tagIds,
                    model.SearchName,
                    model.SearchCity,
                    model.SearchCountry
                );

                if (matchedUsersIds == null || matchedUsersIds.Count == 0)
                {
                    _logger.LogInformation("Search by user {UserId} had 0 results.", userId);
            
                    HttpContext.Session.SetString("matchingUsers", "[]");
                    HttpContext.Session.SetInt32("currentIndex", -1);
            
                    return RedirectToAction("ShowUser", "Browser"); 
                }

                _logger.LogInformation("Search by user {UserId} had {Count} results.", userId, matchedUsersIds.Count);
                HttpContext.Session.SetString("matchingUsers", JsonConvert.SerializeObject(matchedUsersIds));
                HttpContext.Session.SetInt32("currentIndex", 0);

                return RedirectToAction("ShowUser", "Browser");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the search process for user {UserId}.", userId);
                return RedirectToAction("SearchUsersByTags");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ShowUser()
        {
            var userId = _userManager.GetUserId(User);
            try
            {
                var matchingUsers = HttpContext.Session.GetString("matchingUsers");

                if (string.IsNullOrEmpty(matchingUsers))
                {
                    _logger.LogWarning("ShowUser failed: Search session expired or is empty for user {UserId}.", userId);
                    TempData["ErrorMessage"] = "Your search session has expired";
                    return RedirectToAction("SearchUsersByTags");
                }

                var matchingUsersIds = JsonConvert.DeserializeObject<List<string>>(matchingUsers);

                int currentIndex = HttpContext.Session.GetInt32("currentIndex") ?? 0;

                if(currentIndex == -1 || currentIndex >= matchingUsersIds.Count)
                {
                    return View("NoSearchResults");
                }

                var targetUserId = matchingUsersIds[currentIndex];

                var model = await _matchmakingService.MatchedUser(targetUserId);

                if (model == null)
                {
                    _logger.LogWarning("Matched user {TargetUserId} not found in database.", targetUserId);
                    return View("NoSearchResults");
                }

                return View("SearchResults", model);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load matched user profile for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load user profile.";
                return View("NoSearchResults");
            }
        }

        [HttpPost]
        public IActionResult NextUser()
        {
            var userId = _userManager.GetUserId(User);
            try
            {
                var matchingUsers = HttpContext.Session.GetString("matchingUsers");

                if (string.IsNullOrEmpty(matchingUsers))
                {
                    _logger.LogWarning("NextUser failed: Search session expired for user {UserId}.", userId);
                    TempData["ErrorMessage"] = "Your search session has expired.";
                    return RedirectToAction("SearchUsersByTags");
                }

                var matchingUsersIds = JsonConvert.DeserializeObject<List<string>>(matchingUsers);

                int currentIndex = HttpContext.Session.GetInt32("currentIndex") ?? 0;

                currentIndex++;

                if (currentIndex >= matchingUsersIds.Count)
                {
                    _logger.LogInformation("User {UserId} reached the end of the search results.", userId);
                    currentIndex = -1;
                }

                HttpContext.Session.SetInt32("currentIndex", currentIndex);

                return RedirectToAction("ShowUser");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while loading the next user for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load user.";
                return RedirectToAction("SearchUsersByTags");
            }
        }

        // Friendship Service //

        [HttpPost]
        public async Task<IActionResult> SendFriendRequest(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User {CurrentUserId} attempted to send a friend request without specifying a target user.", currentUserId);
                TempData["ErrorMessage"] = "Could not identify the user.";
                return NextUser();
            }

            try
            {
                _logger.LogInformation("User {CurrentUserId} successfully sent a friend request to {TargetUserId}.", currentUserId, userId);
                await _friendshipService.SendRequest(currentUserId, userId);
                TempData["SuccessMessage"] = "Friend request sent successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send friend request from user {CurrentUserId} to {TargetUserId}.", currentUserId, userId);
                TempData["ErrorMessage"] = "Failed to send friend request.";
            }

            return NextUser();
        }

        // AI Matchmaking Service //

        [HttpGet]
        public async Task<IActionResult> SearchWithAI()
        {
            var userId = _userManager.GetUserId(User);
            try
            {
                _logger.LogInformation("User {UserId} initiated AI matchmaking.", userId);
                var matchingUsers = await _aiMatchmakingService.AiMatches(userId);

                if (matchingUsers == null || matchingUsers.Count == 0)
                {
                    _logger.LogInformation("AI matchmaking for user {UserId} found 0 results.", userId);

                    HttpContext.Session.SetString("matchingUsers", "[]");
                    HttpContext.Session.SetInt32("currentIndex", -1);

                    return RedirectToAction("ShowUserWithAI");
                }

                HttpContext.Session.SetString("matchingUsers", JsonConvert.SerializeObject(matchingUsers));
                HttpContext.Session.SetInt32("currentIndex", 0);

                _logger.LogInformation("AI matchmaking for user {UserId} found {Count} results.", userId, matchingUsers.Count);
                return RedirectToAction("ShowUserWithAI");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI matchmaking failed for user {UserId}.", userId);

                HttpContext.Session.SetString("matchingUsers", "[]");
                HttpContext.Session.SetInt32("currentIndex", -1);

                return RedirectToAction("ShowUserWithAI");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ShowUserWithAI()
        {
            var currentUserId = _userManager.GetUserId(User);
            try
            {
                var matchingUsers = HttpContext.Session.GetString("matchingUsers");

                if (string.IsNullOrEmpty(matchingUsers))
                {
                    _logger.LogWarning("ShowUserWithAI failed: AI search session expired for user {UserId}.", currentUserId);
                    TempData["ErrorMessage"] = "Your search session has expired.";
                    return RedirectToAction("SearchUsersByTags");
                }

                var matchingUsersIds = JsonConvert.DeserializeObject<List<string>>(matchingUsers);

                if (matchingUsersIds == null || matchingUsersIds.Count == 0)
                {
                    return View("NoSearchResults");
                }

                int currentIndex = HttpContext.Session.GetInt32("currentIndex") ?? 0;

                if (currentIndex == -1 || currentIndex >= matchingUsersIds.Count)
                {
                    return View("NoSearchResults");
                }

                var (matchedUser, newIndex) = await _aiMatchmakingService.AiNext(currentUserId, matchingUsersIds, currentIndex);

                if (matchedUser != null)
                {
                    HttpContext.Session.SetInt32("currentIndex", newIndex);
                    return View("SearchAiResults", matchedUser);
                }

                _logger.LogInformation("User {UserId} reached the end of the AI search results.", currentUserId);
                HttpContext.Session.SetInt32("currentIndex", -1);
                return View("NoSearchResults");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load AI matched user profile for user {UserId}.", currentUserId);
                TempData["ErrorMessage"] = "Failed to load user profile";
                return View("NoSearchResults");
            }
        }
    }
}
