using INZYNIERKA.Domain.Constants;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace INZYNIERKA.Services.Services
{
    public class AccountService<TUser> : IAccountService<TUser> where TUser : User
    {
        private readonly SignInManager<TUser> signInManager;
        private readonly UserManager<TUser> userManager;
        private readonly IEmailService emailService;
        private readonly IMemoryCache cache;

        public AccountService(SignInManager<TUser> signInManager, UserManager<TUser> userManager, IEmailService emailService, IMemoryCache cache)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.emailService = emailService;
            this.cache = cache;
        }

        public async Task<(bool Succeeded, bool IsLockedOut, string? ErrorMessage)> LoginAsync(LoginViewModel model)
        {
            var result = await signInManager.PasswordSignInAsync(model.Name, model.Password, model.RememberMe, false);

            if (result.IsLockedOut)
            {
                return (false, true, "Your account is Locked.");
            }

            if (result.Succeeded)
            {
                return (true, false, null);
            }

            return (false, false, "Wrong username or password.");
        }

        public async Task<(bool Succeeded, IEnumerable<string> Errors)> RegisterAsync(RegisterViewModel model)
        {
            var errors = new List<string>();

            TUser user = Activator.CreateInstance<TUser>();
            user.UserName = model.Name;
            user.Email = model.Email;
            user.PublicDescription = "PublicDescription";
            user.PrivateDescription = "PrivateDescription";
            user.Avatar = AvatarConsts.DefaultAvatar;

            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await signInManager.SignInAsync(user, isPersistent: false);
                return (true, errors);
            }

            return (false, result.Errors.Select(e => e.Description));
        }

        public async Task<(bool Succeeded, string? ErrorMessage)> VerifyEmailAsync(VerifyEmailViewModel model)
        {
            var user = await userManager.FindByEmailAsync(model.Email);

            if (user != null)
            {
                var otp = new Random().Next(100000, 999999).ToString();

                cache.Set($"OTP{model.Email}", otp, TimeSpan.FromMinutes(10));

                bool sended = await emailService.SendEmail(
                    model.Email,
                    "Reset Password",
                    $"OTP code:{otp}. The code is valid for 10 minutes."
                );

                if (!sended)
                {
                    return (false, "Failed to send OTP email.");
                }
            }

            return (true, null);
        }

        public async Task<(bool Succeeded, IEnumerable<string> Errors)> ChangePasswordAsync(ChangePasswordViewModel model)
        {
            var errors = new List<string>();
            string otp = cache.Get<string>($"OTP{model.Email}");

            if (otp == null || otp != model.OtpCode)
            {
                errors.Add("The code is invalid or has expired.");
                return (false, errors);
            }

            var user = await userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                errors.Add("Cannot reset password for the provided email address.");
                return (false, errors);
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                cache.Remove($"OTP{model.Email}");
                return (true, errors);
            }

            return (false, result.Errors.Select(e => e.Description));
        }

        public async Task LogoutAsync()
        {
            await signInManager.SignOutAsync();
        }

        public async Task<(bool Succeeded, string? ErrorMessage)> DeleteAccountAsync(TUser user)
        {
            if (user == null)
            {
                return (false, "User not found.");
            }

            var currentUser = await userManager.GetUserAsync(signInManager.Context.User);

            if (currentUser != user) {
                return (false, "You can only delete your own account.");
            }

            var lockoutResult = await userManager.SetLockoutEnabledAsync(user, true);
            var dateResult = await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

            if (lockoutResult.Succeeded && dateResult.Succeeded)
            {
                await LogoutAsync();
                return (true, null);
            }

            return (false, "Failed to lock out the account.");
        }
    }
}