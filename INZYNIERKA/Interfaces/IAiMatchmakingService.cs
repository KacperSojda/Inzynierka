using INZYNIERKA.ViewModels;

namespace INZYNIERKA.Services
{
    public interface IAiMatchmakingService
    {
        Task<List<string>> GetPotentialMatchesForAiAsync(string currentUserId);
        Task<(UserViewModel MatchedUser, int LastProcessedIndex)> FindNextAiMatchAsync(string currentUserId, List<string> userIds, int startIndex);
    }
}