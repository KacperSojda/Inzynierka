using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace INZYNIERKA.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IAccountService<User> _accountService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(SignInManager<User> signInManager, UserManager<User> userManager, IAccountService<User> accountService, ILogger<AccountController> logger)
        {
            this._signInManager = signInManager;
            this._userManager = userManager;
            this._accountService = accountService;
            this._logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login attempt failed due to invalid model state.");
                return View(model);
            }

            try
            {
                var (result, isLockedOut, errorMessage) = await _accountService.Login(model);

                if (isLockedOut)
                {
                    _logger.LogWarning("Login failed: User account is locked out.");
                    ModelState.AddModelError("", errorMessage ?? "Your account is Locked.");
                    return View(model);
                }

                if (result)
                {
                    _logger.LogInformation("User logged in successfully.");
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogWarning("Login failed: Invalid data provided.");
                ModelState.AddModelError("", errorMessage ?? "Wrong username or password.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the login request.");
                ModelState.AddModelError("", "Error occurred while processing your request.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Registration attempt failed due to invalid model state.");
                return View(model);
            }

            try
            {
                var (result, errors) = await _accountService.Register(model);

                if (result)
                {
                    _logger.LogInformation("New user registered successfully.");
                    return RedirectToAction("EditProfile", "Profile");
                }

                foreach (var error in errors)
                {
                    ModelState.AddModelError("", error);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the registration request.");
                ModelState.AddModelError("", "Error occurred while processing your request.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult VerifyEmail()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var (result, errorMessage) = await _accountService.VerifyEmail(model);

                if (!result)
                {
                    _logger.LogWarning("Failed to send OTP email to {Email}.", model.Email);
                    ModelState.AddModelError("", errorMessage ?? "Failed to send OTP email.");
                    return View(model);
                }

                _logger.LogInformation("Verification OTP code sent successfully to {Email}.", model.Email);
                TempData["SuccessMessage"] = "Verification code has been sent to your email address.";
                return RedirectToAction("ChangePassword", "Account", new { email = model.Email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while sending OTP email to {Email}.", model.Email);
                ModelState.AddModelError("", "Server Error");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ChangePassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Email is required.";
                return RedirectToAction("VerifyEmail", "Account");
            }
            return View(new ChangePasswordViewModel { Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Change password attempt failed due to invalid model state.");
                return View(model);
            }

            try
            {
                var (result, errors) = await _accountService.ChangePassword(model);

                if (result)
                {
                    _logger.LogInformation("Password changed successfully for user {Email}.", model.Email);
                    TempData["SuccessMessage"] = "Your password has been changed successfully.";
                    return RedirectToAction("Login", "Account");
                }

                _logger.LogWarning("Password change failed for user {Email} with validation errors.", model.Email);
                foreach (var error in errors)
                {
                    ModelState.AddModelError("", error);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while changing the password for user {Email}.", model.Email);
                ModelState.AddModelError("", "Server Error");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            var userId = _userManager.GetUserId(User);
            try
            {
                await _accountService.Logout();
                _logger.LogInformation("User {UserId} logged out successfully.", userId);
                TempData["SuccessMessage"] = "You have been successfully logged out.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during logout for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Server error occurred during logout.";
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = _userManager.GetUserId(User);

            try
            {
                if (userId == null)
                {
                    _logger.LogWarning("Delete account failed: User {UserId} not found or session expired.", userId);
                    TempData["ErrorMessage"] = "User not found or session expired.";
                    return NotFound();
                }

                var (result, errorMessage) = _accountService.DeleteAccount(userId);

                if (result)
                {
                    _logger.LogInformation("User account {UserId} was successfully locked out/deleted.", userId);
                    TempData["SuccessMessage"] = "Your account has been locked out.";
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogWarning("Failed to delete account for user {UserId}. Reason: {ErrorMessage}", userId, errorMessage);
                TempData["ErrorMessage"] = "Failed to delete account.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected server error occurred while deleting account for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Server error";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}