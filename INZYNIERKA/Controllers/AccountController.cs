using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace INZYNIERKA.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<User> signInManager;
        private readonly UserManager<User> userManager;
        private readonly IAccountService<User> accountService;

        public AccountController(SignInManager<User> signInManager, UserManager<User> userManager, IAccountService<User> accountService)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.accountService = accountService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var (succeeded, isLockedOut, errorMessage) = await accountService.LoginAsync(model);

                if (isLockedOut)
                {
                    ModelState.AddModelError("", errorMessage ?? "Your account is Locked.");
                    return View(model);
                }

                if (succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", errorMessage ?? "Wrong username or password.");
                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Error occurred while processing your request.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var (succeeded, errors) = await accountService.RegisterAsync(model);

                if (succeeded)
                {
                    return RedirectToAction("EditProfile", "Profile");
                }

                foreach (var error in errors)
                {
                    ModelState.AddModelError("", error);
                }

                return View(model);
            }
            catch (Exception)
            {
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
                var (succeeded, errorMessage) = await accountService.VerifyEmailAsync(model);

                if (!succeeded)
                {
                    ModelState.AddModelError("", errorMessage ?? "Failed to send OTP email.");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Verification code has been sent to your email address.";
                return RedirectToAction("ChangePassword", "Account", new { email = model.Email });
            }
            catch (Exception)
            {
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
            if (!ModelState.IsValid) return View(model);

            try
            {
                var (succeeded, errors) = await accountService.ChangePasswordAsync(model);

                if (succeeded)
                {
                    TempData["SuccessMessage"] = "Your password has been changed successfully.";
                    return RedirectToAction("Login", "Account");
                }

                foreach (var error in errors)
                {
                    ModelState.AddModelError("", error);
                }

                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Server Error");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await accountService.LogoutAsync();
                TempData["SuccessMessage"] = "You have been successfully logged out.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Server error occurred during logout.";
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAccount()
        {
            try
            {
                var user = await userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found or session expired.";
                    return NotFound();
                }

                var (succeeded, errorMessage) = await accountService.DeleteAccountAsync(user);

                if (succeeded)
                {
                    TempData["SuccessMessage"] = "Your account has been locked out.";
                    return RedirectToAction("Index", "Home");
                }

                TempData["ErrorMessage"] = "Failed to delete account.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Server error";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}