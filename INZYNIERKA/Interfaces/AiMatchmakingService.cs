using INZYNIERKA.Data;
using INZYNIERKA.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services
{
    public class AiMatchmakingService : IAiMatchmakingService
    {
        private readonly INZDbContext context;
        private readonly GeminiService geminiService;
        private readonly IConfiguration configuration;

        public AiMatchmakingService(INZDbContext context, GeminiService geminiService, IConfiguration configuration)
        {
            this.context = context;
            this.geminiService = geminiService;
            this.configuration = configuration;
        }

        public async Task<List<string>> GetPotentialMatchesForAiAsync(string currentUserId)
        {
            var connectedUserIds = await context.UserFriends
                .Where(f => f.UserId == currentUserId || f.FriendId == currentUserId)
                .Select(f => f.UserId == currentUserId ? f.FriendId : f.UserId)
                .ToListAsync();

            var matchingUsers = await context.Users
                .Where(u => u.Id != currentUserId && !connectedUserIds.Contains(u.Id))
                .OrderBy(u => Guid.NewGuid())
                .Select(u => u.Id)
                .ToListAsync();

            return matchingUsers;
        }

        public async Task<(UserViewModel MatchedUser, int LastProcessedIndex)> FindNextAiMatchAsync(string currentUserId, List<string> userIds, int startIndex)
        {
            var user = await context.Users
                .Include(u => u.UserTags).ThenInclude(ut => ut.Tag)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (user == null) return (null, startIndex);

            var tags = user.UserTags.Select(ut => ut.Tag.Name).ToList();
            var combinedString = $"First Description: {user.PublicDescription} {user.PrivateDescription} Hobby: {string.Join(", ", tags)}";
            var browserPrompt = configuration["Prompts:Browser"];

            int currentIndex = startIndex;

            while (currentIndex < userIds.Count)
            {
                var targetUserId = userIds[currentIndex];
                currentIndex++;

                var dbUser = await context.Users
                    .Include(u => u.UserTags).ThenInclude(ut => ut.Tag)
                    .FirstOrDefaultAsync(u => u.Id == targetUserId);

                if (dbUser == null) continue;

                var friendTags = dbUser.UserTags.Select(ut => ut.Tag.Name);
                var friendCombinedString = $"Second Description: {dbUser.PublicDescription} Hobby: {string.Join(", ", friendTags)}";

                var promptString = combinedString + " " + friendCombinedString;

                var geminiAns = await geminiService.AskAsync(promptString, browserPrompt);

                if (geminiAns.Trim().ToUpper().Contains("YES"))
                {
                    var model = new UserViewModel
                    {
                        Id = dbUser.Id,
                        UserName = dbUser.UserName,
                        Avatar = dbUser.Avatar,
                        PublicDescription = dbUser.PublicDescription,
                        Tags = dbUser.UserTags.Select(ut => ut.Tag.Name).ToList()
                    };

                    return (model, currentIndex);
                }

                await Task.Delay(15);
            }

            return (null, currentIndex);
        }
    }
}