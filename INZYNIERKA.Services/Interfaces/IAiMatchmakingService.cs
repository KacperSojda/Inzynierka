using INZYNIERKA.Services.ViewModels;

namespace INZYNIERKA.Services.Interfaces
{
    public interface IAiMatchmakingService
    {
        Task<List<string>> GetPotentialMatchesForAiAsync(string currentUserId);
        Task<(UserViewModel MatchedUser, int LastProcessedIndex)> FindNextAiMatchAsync(string currentUserId, List<string> userIds, int startIndex);
    }
}