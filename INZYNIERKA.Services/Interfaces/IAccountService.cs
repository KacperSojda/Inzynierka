using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.ViewModels;

namespace INZYNIERKA.Services.Interfaces
{
    public interface IAccountService<TUser> where TUser : User
    {
        Task<(bool Succeeded, bool IsLockedOut, string? ErrorMessage)> LoginAsync(LoginViewModel model);
        Task<(bool Succeeded, IEnumerable<string> Errors)> RegisterAsync(RegisterViewModel model);
        Task<(bool Succeeded, string? ErrorMessage)> VerifyEmailAsync(VerifyEmailViewModel model);
        Task<(bool Succeeded, IEnumerable<string> Errors)> ChangePasswordAsync(ChangePasswordViewModel model);
        Task LogoutAsync();
        Task<(bool Succeeded, string? ErrorMessage)> DeleteAccountAsync(TUser user);
    }
}