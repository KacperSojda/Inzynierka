using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.ViewModels;

namespace INZYNIERKA.Services.Interfaces
{
    public interface IAccountService<TUser> where TUser : User
    {
        Task<(bool Result, bool IsLockedOut, string? ErrorMessage)> Login(LoginViewModel model);
        Task<(bool Result, IEnumerable<string> Errors)> Register(RegisterViewModel model);
        Task<(bool Result, string? ErrorMessage)> VerifyEmail(VerifyEmailViewModel model);
        Task<(bool Result, IEnumerable<string> Errors)> ChangePassword(ChangePasswordViewModel model);
        Task Logout();
        Task<(bool Result, string? ErrorMessage)> DeleteAccount(TUser user);
    }
}