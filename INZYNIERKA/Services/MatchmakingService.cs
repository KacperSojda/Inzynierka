using INZYNIERKA.Data;
using INZYNIERKA.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services
{
    public class MatchmakingService : IMatchmakingService
    {
        private readonly INZDbContext context;

        public MatchmakingService(INZDbContext context)
        {
            this.context = context;
        }

        public async Task<SearchByTagsViewModel> GetTagsForSearchAsync()
        {
            var tags = await context.Tags.ToListAsync();

            return new SearchByTagsViewModel
            {
                AvailableTags = tags.Select(t => new TagCheckboxItem
                {
                    TagId = t.Id,
                    TagName = t.Name,
                    IsSelected = false
                }).ToList()
            };
        }

        public async Task<List<string>> GetMatchingUserIdsByTagsAsync(string currentUserId, List<int> selectedTagIds)
        {
            var connectedUserIds = await context.UserFriends
                .Where(f => f.UserId == currentUserId || f.FriendId == currentUserId)
                .Select(f => f.UserId == currentUserId ? f.FriendId : f.UserId)
                .ToListAsync();

            var matchingUserIds = await context.Users
                .Where(u =>
                    u.Id != currentUserId &&
                    !connectedUserIds.Contains(u.Id) &&
                    u.UserTags.Any(ut => selectedTagIds.Contains(ut.TagId)))
                .Select(u => u.Id)
                .ToListAsync();

            var random = new Random();
            return matchingUserIds.OrderBy(id => random.Next()).ToList();
        }

        public async Task<UserViewModel> GetUserForBrowserAsync(string targetUserId)
        {
            var user = await context.Users
                .Include(u => u.UserTags)
                    .ThenInclude(ut => ut.Tag)
                .FirstOrDefaultAsync(u => u.Id == targetUserId);

            if (user == null) return null;

            return new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Avatar = user.Avatar,
                PublicDescription = user.PublicDescription,
                Tags = user.UserTags.Select(ut => ut.Tag.Name).ToList()
            };
        }
    }
}