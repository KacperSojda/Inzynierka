using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace INZYNIERKA.Controllers
{
    [Authorize]
    public class TagsController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly ITagService<User> tagService;
        private readonly ILogger<TagsController> logger;

        public TagsController(UserManager<User> userManager, ITagService<User> tagService, ILogger<TagsController> logger)
        {
            this.userManager = userManager;
            this.tagService = tagService;
            this.logger = logger;
        }
        public async Task<IActionResult> SelectTags()
        {
            var userId = userManager.GetUserId(User);

            try
            {
                var model = await tagService.UserTags(userId);
                logger.LogInformation("User {UserId} accessed their tags selection page.", userId);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load tags for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load tags.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SelectTags(SelectTagsViewModel model)
        {
            var userId = userManager.GetUserId(User);

            if (model == null)
            {
                logger.LogWarning("SelectTags (POST) failed: Model is null for user {UserId}.", userId);
                TempData["ErrorMessage"] = "No tags data provided.";
                return RedirectToAction("Index");
            }

            try
            {
                var selectedTagsIds = model.Tags
                    .Where(t => t.Selected)
                    .Select(t => t.TagId)
                    .ToList();

                await tagService.UpdateUserTags(userId, selectedTagsIds);
                logger.LogInformation("User {UserId} updated their tags successfully.", userId);

                TempData["SuccessMessage"] = "Your tags have been updated!";
                return RedirectToAction("Index", "Profile");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server error while updating tags for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Server error while updating tags.";
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
            var userId = userManager.GetUserId(User);

            if (model == null || !ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var (result, errorMessage) = await tagService.NewTag(model.TagName);
                if (result)
                {
                    logger.LogInformation("User {UserId} added new tag '{TagName}' successfully.", userId, model.TagName);
                    TempData["SuccessMessage"] = "Tag added successfully";
                    return RedirectToAction("Index", "Profile");
                }

                logger.LogWarning("User {UserId} failed to create tag '{TagName}'. Reason: {ErrorMessage}", userId, model.TagName, errorMessage);
                ModelState.AddModelError("", "Failed to add tag.");
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server error while adding tag for user {UserId}.", userId);
                ModelState.AddModelError("", "Server error");
                return View(model);
            }
        }

        public async Task<IActionResult> ShowTags()
        {
            var userId = userManager.GetUserId(User);
            try
            {
                var tags = await tagService.AllTags();
                logger.LogInformation("User {UserId} requested the list of all tags.", userId);
                return View(tags);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load tags list for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load tags list.";
                return RedirectToAction("Index");
            }
        }
    }
}
