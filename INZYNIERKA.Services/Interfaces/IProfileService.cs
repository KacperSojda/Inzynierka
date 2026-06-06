using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.ViewModels;

namespace INZYNIERKA.Services.Interfaces
{
    public interface IProfileService<TUser> where TUser : User
    {
        Task<UserViewModel> Profile(string userId);
        Task<UserViewModel> EditProfile(string userId);
        Task<UserViewModel> OtherProfile(string targetUserId);
        Task<(bool Result, string ErrorMessage)> UpdateProfile(string userId, UserViewModel model);
        Task<bool> UpdateAvatar(string userId, string avatarData);
        Task<bool> UpdateCover(string userId, string coverData);
    }
}