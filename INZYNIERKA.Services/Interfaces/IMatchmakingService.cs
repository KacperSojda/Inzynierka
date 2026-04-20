using INZYNIERKA.Services.ViewModels;

namespace INZYNIERKA.Services.Interfaces
{
    public interface IMatchmakingService
    {
        Task<SearchByTagsViewModel> GetTagsForSearchAsync();
        Task<List<string>> GetMatchingUserIdsByTagsAsync(string currentUserId, List<int> selectedTagIds);
        Task<UserViewModel> GetUserForBrowserAsync(string targetUserId);
    }
}