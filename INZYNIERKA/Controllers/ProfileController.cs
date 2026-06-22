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
        private readonly UserManager<User> _userManager;
        private readonly IFileService _fileService;
        private readonly IProfileService<User> _profileService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            UserManager<User> userManager, 
            IFriendshipService<User> friendshipService,
            INotificationService<User> notificationService,
            ITagService<User> tagService,
            IFileService fileService,
            IProfileService<User> profileService,
            ILogger<ProfileController> logger)
        {
            this._userManager = userManager;
            this._fileService = fileService;
            this._profileService = profileService;
            this._logger = logger;
        }

        // Profile Service //
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            try
            {
                var model = await _profileService.Profile(userId);
                if (model == null)
                {
                    _logger.LogWarning("Profile Index failed: User profile not found for {UserId}.", userId);
                    TempData["ErrorMessage"] = "Cannot find your user profile.";
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogInformation("User {UserId} viewed their profile.", userId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load profile data for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load profile data.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userId = _userManager.GetUserId(User);

            try
            {
                var model = await _profileService.EditProfile(userId);
                if (model == null)
                {
                    _logger.LogWarning("EditProfile failed: User profile not found for {UserId}.", userId);
                    TempData["ErrorMessage"] = "Cannot find your user profile.";
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogInformation("User {UserId} accessed profile edit page.", userId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load edit profile form for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load the edit form.";
                return RedirectToAction("Index", "Home");
            }

        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(UserViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (model == null)
            {
                _logger.LogWarning("EditProfile (POST) failed: Model is null for user {UserId}.", userId);
                return RedirectToAction("Index, Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var (result, errorMessage) = await _profileService.UpdateProfile(userId, model);

                if (result)
                {
                    _logger.LogInformation("User {UserId} successfully updated their profile.", userId);
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToAction("Index");
                }

                _logger.LogWarning("User {UserId} profile update failed. Reason: {ErrorMessage}", userId, errorMessage);
                ModelState.AddModelError("", "Update failed.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error while updating profile for user {UserId}.", userId);
                ModelState.AddModelError("", "Server error");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ShowProfile(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("ShowProfile failed: userId parameter is null or empty. Current user: {CurrentUserId}.", currentUserId);
                TempData["ErrorMessage"] = "Cannot identify user";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var model = await _profileService.OtherProfile(userId, currentUserId);

                if (model == null)
                {
                    _logger.LogWarning("ShowProfile failed: Profile {TargetUserId} not found (Requested by: {CurrentUserId}).", userId, currentUserId);
                    TempData["ErrorMessage"] = "User profile not found.";
                    return NotFound("Cannot find user profile.");
                }

                _logger.LogInformation("User {CurrentUserId} viewed profile of user {TargetUserId}.", currentUserId, userId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load profile for user {TargetUserId} (Requested by: {CurrentUserId}).", userId, currentUserId);
                TempData["ErrorMessage"] = "Failed to load the user's profile.";
                return RedirectToAction("Index", "Home");
            }
        }

        // File Service // 

        [HttpGet]
        public async Task<IActionResult> EditMedia()
        {
            var userId = _userManager.GetUserId(User);

            try
            {
                var model = await _profileService.Profile(userId);
                if (model == null)
                {
                    _logger.LogWarning("EditMedia failed: Profile not found for user {UserId}.", userId);
                    TempData["ErrorMessage"] = "User profile not found.";
                    return RedirectToAction("Index");
                }

                _logger.LogInformation("User {UserId} accessed media editor.", userId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load media editor for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load media editor.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditAvatar(IFormFile avatarFile)
        {
            var userId = _userManager.GetUserId(User);

            if (avatarFile == null || avatarFile.Length == 0)
            {
                _logger.LogWarning("EditAvatar failed: No file uploaded by user {UserId}.", userId);
                TempData["ErrorMessage"] = "unvalid image file.";
                return RedirectToAction("EditMedia");
            }

            try
            {
                var (result, avatar) = await _fileService.UploadFile(avatarFile);

                if (!result)
                {
                    _logger.LogWarning("EditAvatar failed: File upload failed for user {UserId}.", userId);
                    TempData["ErrorMessage"] = "Failed to upload the image.";
                    return RedirectToAction("EditMedia");
                }

                var avatarResult = await _profileService.UpdateAvatar(userId, avatar);

                if (!avatarResult)
                {
                    _logger.LogInformation("User {UserId} failed to update their avatar.", userId);
                    TempData["ErrorMessage"] = "Failed to update profile picture.";
                    return RedirectToAction("EditMedia");
                }

                _logger.LogInformation("User {UserId} successfully updated their avatar.", userId);
                TempData["SuccessMessage"] = "Profile picture updated successfully!";
                return RedirectToAction("EditMedia");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error while saving avatar for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Server error while saving the avatar.";
                return RedirectToAction("EditMedia");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditCover(IFormFile coverFile)
        {
            var userId = _userManager.GetUserId(User);

            if (coverFile == null || coverFile.Length == 0)
            {
                _logger.LogWarning("EditCover failed: No file uploaded by user {UserId}.", userId);
                TempData["ErrorMessage"] = "unvalid image file.";
                return RedirectToAction("EditMedia");
            }

            try
            {
                var (result, cover) = await _fileService.UploadFile(coverFile);

                if (!result)
                {
                    _logger.LogWarning("EditCover failed: File upload failed for user {UserId}.", userId);
                    TempData["ErrorMessage"] = "Failed to upload the image";
                    return RedirectToAction("EditMedia");
                }

                var coverResult = await _profileService.UpdateCover(userId, cover);

                if (!coverResult)
                {
                    _logger.LogInformation("User {UserId} failed to update their cover photo.", userId);
                    TempData["ErrorMessage"] = "Failed to update cover photo.";
                    return RedirectToAction("EditMedia");
                }

                _logger.LogInformation("User {UserId} successfully updated their cover photo.", userId);
                TempData["SuccessMessage"] = "Cover photo updated successfully.";
                return RedirectToAction("EditMedia");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error while saving cover photo for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Server error";
                return RedirectToAction("EditMedia");
            }
        }
    }
}
