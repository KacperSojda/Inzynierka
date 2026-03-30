using INZYNIERKA.ViewModels;

namespace INZYNIERKA.Services
{
    public interface IProfileService
    {
        Task<UserViewModel> GetUserProfileAsync(string userId);
        Task<UserViewModel> GetUserProfileForEditAsync(string userId);
        Task<UserViewModel> GetOtherUserProfileAsync(string targetUserId);
        Task<(bool IsSuccess, IEnumerable<string> Errors)> UpdateUserProfileAsync(string userId, UserViewModel model);
    }
}