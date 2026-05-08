using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace INZYNIERKA.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<User> signInManager;
        private readonly UserManager<User> userManager;
        private readonly IEmailService emailService;
        private readonly IMemoryCache cache;

        public AccountController(SignInManager<User> signInManager, UserManager<User> userManager, IEmailService emailService, IMemoryCache cache)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.emailService = emailService;
            this.cache = cache;
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

            try {
                var result = await signInManager.PasswordSignInAsync(model.Name, model.Password, model.RememberMe, false);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Name or password incorrect");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Serwer Error");
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
                User user = new User
                {
                    UserName = model.Name,
                    Email = model.Email,
                    PublicDescription = "PublicDescription",
                    PrivateDescription = "PrivateDescription",
                    Avatar = "Avatar",
                };

                var result = await userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    return RedirectToAction("Login", "Account");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Serwer Error");
                return View(model);
            }
        }
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
                var user = await userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    ModelState.AddModelError("", "Wrong Email");
                }
                else
                {
                    var otp = new Random().Next(100000, 999999).ToString();

                    cache.Set($"ResetOTP_{model.Email}", otp, TimeSpan.FromMinutes(10));

                    bool isSent = await emailService.SendEmailAsync(
                        model.Email,
                        "Password Reset",
                        $"Your OTP code is: <strong>{otp}</strong>. The code is valid for 10 minutes."
                    );

                    if (!isSent)
                    {
                        ModelState.AddModelError("", "Failed to send email. Please try again later.");
                        return View(model);
                    }
                }

                return RedirectToAction("ChangePassword", "Account", new { email = model.Email });

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Serwer Error");
                return View(model);
            }
        }

        public IActionResult ChangePassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("VerifyEmail", "Account");
            }
            return View(new ChangePasswordViewModel {Email = email});
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {

                if (!cache.TryGetValue($"ResetOTP_{model.Email}", out string savedOtp))
                {
                    ModelState.AddModelError("", "OTP code expired or not found");
                    return View(model);
                }

                if (savedOtp != model.OtpCode)
                {
                    ModelState.AddModelError("", "Invalid OTP code");
                    return View(model);
                }

                var user = await userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "Email not Found");
                    return View(model);
                }

                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var result = await userManager.ResetPasswordAsync(user, token, model.NewPassword);

                if (result.Succeeded)
                {
                    cache.Remove($"ResetOTP_{model.Email}");

                    return RedirectToAction("Login", "Account");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Serwer Error");
                return View(model);
            }

        }

        public async Task<IActionResult> Logout()
        {
            try
            {
                await signInManager.SignOutAsync();
            }
            catch (Exception ex)
            {
                
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
