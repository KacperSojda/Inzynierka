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
        private readonly SignInManager<TUser> _signInManager;
        private readonly UserManager<TUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;

        public AccountService(SignInManager<TUser> signInManager, UserManager<TUser> userManager, IEmailService emailService, IMemoryCache cache)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailService = emailService;
            _cache = cache;
        }

        /// <summary>
        /// Uwierzytelnia użytkownika w systemie na podstawie przekazanych danych logowania.
        /// Sprawdza poprawność danych uwierzytelniających oraz weryfikuje, czy konto nie jest zablokowane.
        /// </summary>
        /// <returns>
        /// Krotka (Tuple) zawierająca wynik operacji:
        /// <list type="bullet">
        /// <item>
        /// <description><c>Result</c>: <c>true</c> jeśli logowanie zakończyło się sukcesem, w przeciwnym razie <c>false</c>.</description>
        /// </item>
        /// <item>
        /// <description><c>IsLockedOut</c>: <c>true</c> jeśli konto użytkownika zostało tymczasowo zablokowane (np. po zbyt wielu błędnych próbach).</description>
        /// </item>
        /// <item>
        /// <description><c>ErrorMessage</c>: Komunikat błędu wyjaśniający powód niepowodzenia (np. zablokowane konto lub błędne dane logowania).</description>
        /// </item>
        /// </list>
        /// </returns>
        public async Task<(bool Result, bool IsLockedOut, string? ErrorMessage)> Login(LoginViewModel model)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Name, model.Password, model.RememberMe, false);

            if (result.IsLockedOut)
            {
                return (false, true, "Your account is Locked.");
            }

            if (result.Succeeded)
            {
                return (true, false, "");
            }

            return (false, false, "Wrong username or password.");
        }

        public async Task<(bool Result, IEnumerable<string> Errors)> Register(RegisterViewModel model)
        {
            var errors = new List<string>();

            TUser user = Activator.CreateInstance<TUser>();
            user.UserName = model.Name;
            user.Email = model.Email;
            user.PublicDescription = "PublicDescription";
            user.PrivateDescription = "PrivateDescription";
            user.Avatar = AvatarConsts.DefaultAvatar;

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return (true, errors);
            }

            return (false, result.Errors.Select(e => e.Description));
        }

        public async Task<(bool Result, string? ErrorMessage)> VerifyEmail(VerifyEmailViewModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null)
            {
                var otp = new Random().Next(100000, 999999).ToString();

                _cache.Set($"OTP{model.Email}", otp, TimeSpan.FromMinutes(10));

                bool sent = await _emailService.SendEmail(
                    model.Email,
                    "Reset Password",
                    $"OTP code:{otp}. The code is valid for 10 minutes."
                );

                if (!sent)
                {
                    return (false, "Failed to send OTP email.");
                }
            }

            return (true, null);
        }

        public async Task<(bool Result, IEnumerable<string> Errors)> ChangePassword(ChangePasswordViewModel model)
        {
            var errors = new List<string>();
            string otp = _cache.Get<string>($"OTP{model.Email}");

            if (otp == null || otp != model.OtpCode)
            {
                errors.Add("The code is invalid or has expired.");
                return (false, errors);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                errors.Add("Cannot reset password for the provided email address.");
                return (false, errors);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                _cache.Remove($"OTP{model.Email}");
                return (true, errors);
            }

            return (false, result.Errors.Select(e => e.Description));
        }

        public async Task Logout()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<(bool Result, string? ErrorMessage)> DeleteAccount(TUser user)
        {
            var currentUser = await _userManager.GetUserAsync(_signInManager.Context.User);

            if (currentUser == null || currentUser.Id != user.Id)
            {
                return (false, "You can only delete your own account.");
            }

            var lockoutResult = await _userManager.SetLockoutEnabledAsync(user, true);
            var dateResult = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

            if (lockoutResult.Succeeded && dateResult.Succeeded)
            {
                await Logout();
                return (true, null);
            }

            return (false, "Failed to lock out the account.");
        }
    }
}