using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.ViewModels;
using INZYNIERKA.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace INZYNIERKA.Services.Services
{
    public class AiMatchmakingService<TUser> : IAiMatchmakingService<TUser> where TUser : User
    {
        private readonly INZDbContext<TUser> _context;
        private readonly IGeminiService _geminiService;
        private readonly IConfiguration _configuration;

        public AiMatchmakingService(INZDbContext<TUser> context, IGeminiService geminiService, IConfiguration configuration)
        {
            _context = context;
            _geminiService = geminiService; 
            _configuration = configuration;
        }

        /// <summary>Retrieves a randomized list of potential user IDs who are not already friends with the current user.</summary>
        /// <param name="currentUserId">The ID of the user looking for matches.</param>
        /// <returns>A list of potential match users IDs.</returns>
        public async Task<List<string>> AiMatches(string currentUserId)
        {
            var connectedUserIds = await _context.UserFriends
                .Where(f => f.UserId == currentUserId || f.FriendId == currentUserId)
                .Select(f => f.UserId == currentUserId ? f.FriendId : f.UserId)
                .ToListAsync();

            var matchingUsers = await _context.Users
                .Where(u => u.Id != currentUserId && !connectedUserIds.Contains(u.Id))
                .OrderBy(u => Guid.NewGuid())
                .Select(u => u.Id)
                .ToListAsync();

            return matchingUsers;
        }

        /// <summary>Evaluates potential matches using AI and returns the next user found.</summary>
        /// <param name="currentUserId">The ID of the current user.</param>
        /// <param name="userIds">The list of potential match IDs.</param>
        /// <param name="startIndex">The index in the list to start from.</param>
        /// <returns>The matched user's profile and the index of the last processed user.</returns>
        public async Task<(UserViewModel MatchedUser, int LastProcessedIndex)> AiNext(string currentUserId, List<string> userIds, int startIndex)
        {
            var user = await _context.Users
                .Include(u => u.UserTags).ThenInclude(ut => ut.Tag)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (user == null) return (null, startIndex);

            var tags = user.UserTags.Select(ut => ut.Tag.Name).ToList();
            var combinedString = $"Description: {user.PublicDescription} {user.PrivateDescription} Hobby: {string.Join(", ", tags)}";
            var browserPrompt = _configuration["Prompts:Browser"];

            int currentIndex = startIndex;

            while (currentIndex < userIds.Count)
            {
                var targetUserId = userIds[currentIndex];
                currentIndex++;

                var dbUser = await _context.Users
                    .Include(u => u.UserTags).ThenInclude(ut => ut.Tag)
                    .FirstOrDefaultAsync(u => u.Id == targetUserId);

                if (dbUser == null) continue;

                var friendTags = dbUser.UserTags.Select(ut => ut.Tag.Name);
                var friendCombinedString = $"Description: {dbUser.PublicDescription} Hobby: {string.Join(", ", friendTags)}";

                string finalPrompt = browserPrompt
                    .Replace("{person1}", combinedString)
                    .Replace("{person2}", friendCombinedString);

                var geminiAns = await _geminiService.AskAsync(string.Empty, finalPrompt);

                if (!string.IsNullOrWhiteSpace(geminiAns) && geminiAns.Trim().ToUpper().Contains("YES"))
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
            }

            return (null, currentIndex);
        }
    }
}