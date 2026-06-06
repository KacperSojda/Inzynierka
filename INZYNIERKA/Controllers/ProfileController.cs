using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace INZYNIERKA.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly IFriendshipService<User> friendshipService;
        private readonly INotificationService<User> notificationService;
        private readonly ITagService<User> tagService;
        private readonly IFileService fileService;
        private readonly IProfileService<User> profileService;
        private readonly ILogger<ProfileController> logger;

        public ProfileController(
            UserManager<User> userManager, 
            IFriendshipService<User> friendshipService,
            INotificationService<User> notificationService,
            ITagService<User> tagService,
            IFileService fileService,
            IProfileService<User> profileService,
            ILogger<ProfileController> logger)
        {
            this.userManager = userManager;
            this.friendshipService = friendshipService;
            this.notificationService = notificationService;
            this.tagService = tagService;
            this.fileService = fileService;
            this.profileService = profileService;
            this.logger = logger;
        }

        // Profile Service //
        public async Task<IActionResult> Index()
        {
            var userId = userManager.GetUserId(User);

            try
            {
                var model = await profileService.Profile(userId);

                if (model == null)
                {
                    logger.LogWarning("Profile Index failed: User profile not found for {UserId}.", userId);
                    TempData["ErrorMessage"] = "Cannot find your user profile.";
                    return RedirectToAction("Index", "Home");
                }

                logger.LogInformation("User {UserId} viewed their profile.", userId);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load profile data for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load profile data.";
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<IActionResult> EditProfile()
        {
            var userId = userManager.GetUserId(User);

            try
            {
                var model = await profileService.EditProfile(userId);

                if (model == null)
                {
                    logger.LogWarning("EditProfile failed: User profile not found for {UserId}.", userId);
                    TempData["ErrorMessage"] = "Cannot find your user profile.";
                    return RedirectToAction("Index", "Home");
                }

                logger.LogInformation("User {UserId} accessed profile edit page.", userId);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load edit profile form for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load the edit form.";
                return RedirectToAction("Index", "Home");
            }

        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(UserViewModel model)
        {
            var userId = userManager.GetUserId(User);

            if (model == null)
            {
                logger.LogWarning("EditProfile (POST) failed: Model is null for user {UserId}.", userId);
                return RedirectToAction("Index, Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var (result, errorMessage) = await profileService.UpdateProfile(userId, model);

                if (result)
                {
                    logger.LogInformation("User {UserId} successfully updated their profile.", userId);
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToAction("Index");
                }

                logger.LogWarning("User {UserId} profile update failed. Reason: {ErrorMessage}", userId, errorMessage);
                ModelState.AddModelError("", "Update failed.");
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server error while updating profile for user {UserId}.", userId);
                ModelState.AddModelError("", "Server error");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ShowProfile(string userId)
        {
            var currentUserId = userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                logger.LogWarning("ShowProfile failed: userId parameter is null or empty. Current user: {CurrentUserId}.", currentUserId);
                TempData["ErrorMessage"] = "Cannot identify user";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var model = await profileService.OtherProfile(userId);

                if (model == null)
                {
                    logger.LogWarning("ShowProfile failed: Profile {TargetUserId} not found (Requested by: {CurrentUserId}).", userId, currentUserId);
                    TempData["ErrorMessage"] = "User profile not found.";
                    return NotFound("Cannot find user profile.");
                }

                logger.LogInformation("User {CurrentUserId} viewed profile of user {TargetUserId}.", currentUserId, userId);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load profile for user {TargetUserId} (Requested by: {CurrentUserId}).", userId, currentUserId);
                TempData["ErrorMessage"] = "Failed to load the user's profile.";
                return RedirectToAction("Index", "Home");
            }
        }

        // File Service // 

        [HttpGet]
        public async Task<IActionResult> EditMedia()
        {
            var userId = userManager.GetUserId(User);

            try
            {
                var model = await profileService.Profile(userId);

                if (model == null)
                {
                    logger.LogWarning("EditMedia failed: Profile not found for user {UserId}.", userId);
                    TempData["ErrorMessage"] = "User profile not found.";
                    return RedirectToAction("Index");
                }

                logger.LogInformation("User {UserId} accessed media editor.", userId);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load media editor for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load media editor.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditAvatar(IFormFile AvatarFile)
        {
            var userId = userManager.GetUserId(User);

            if (AvatarFile == null || AvatarFile.Length == 0)
            {
                logger.LogWarning("EditAvatar failed: No file uploaded by user {UserId}.", userId);
                TempData["ErrorMessage"] = "unvalid image file.";
                return RedirectToAction("EditMedia");
            }

            try
            {
                var (result, avatar) = await fileService.UploadFile(AvatarFile);

                if (!result)
                {
                    logger.LogWarning("EditAvatar failed: File upload failed for user {UserId}.", userId);
                    TempData["ErrorMessage"] = "Failed to upload the image.";
                    return RedirectToAction("EditMedia");
                }

                result = await profileService.UpdateAvatar(userId, avatar);

                logger.LogInformation("User {UserId} successfully updated their avatar.", userId);
                TempData["SuccessMessage"] = "Profile picture updated successfully!";
                return RedirectToAction("EditMedia");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server error while saving avatar for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Server error while saving the avatar.";
                return RedirectToAction("EditMedia");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditCover(IFormFile CoverFile)
        {
            var userId = userManager.GetUserId(User);

            if (CoverFile == null || CoverFile.Length == 0)
            {
                logger.LogWarning("EditCover failed: No file uploaded by user {UserId}.", userId);
                TempData["ErrorMessage"] = "unvalid image file.";
                return RedirectToAction("EditMedia");
            }

            try
            {
                var (result, cover) = await fileService.UploadFile(CoverFile);

                if (!result)
                {
                    logger.LogWarning("EditCover failed: File upload failed for user {UserId}.", userId);
                    TempData["ErrorMessage"] = "Failed to upload the image";
                    return RedirectToAction("EditMedia");
                }

                var success = await profileService.UpdateCover(userId, cover);
                logger.LogInformation("User {UserId} successfully updated their cover photo.", userId);

                TempData["SuccessMessage"] = "Cover photo updated successfully.";
                return RedirectToAction("EditMedia");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server error while saving cover photo for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Server error";
                return RedirectToAction("EditMedia");
            }
        }
    }
}
