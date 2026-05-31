using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.ViewModels;

namespace INZYNIERKA.Services.Interfaces
{
    public interface IMatchmakingService<TUser> where TUser : User
    {
        Task<SearchByTagsViewModel> Tags();
        Task<List<string>> MatchingUsersIds(string currentUserId, List<int> selectedTagIds, string? searchName = null, string? searchCity = null, string? searchCountry = null);
        Task<UserViewModel> MatchedUser(string targetUserId);
    }
}