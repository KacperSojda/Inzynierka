using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.ViewModels;

namespace INZYNIERKA.Services.Interfaces
{
    public interface IAiMatchmakingService<TUser> where TUser : User
    {
        Task<List<string>> AiMatches(string currentUserId);
        Task<(UserViewModel MatchedUser, int LastProcessedIndex)> AiNext(string currentUserId, List<string> userIds, int startIndex);
    }
}