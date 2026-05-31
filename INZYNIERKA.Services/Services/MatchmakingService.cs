using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services.Services{
    public class MatchmakingService<TUser> : IMatchmakingService<TUser> where TUser : User
    {
        private readonly INZDbContext<TUser> context;

        public MatchmakingService(INZDbContext<TUser> context)
        {
            this.context = context;
        }

        public async Task<SearchByTagsViewModel> Tags()
        {
            var tags = await context.Tags.ToListAsync();

            return new SearchByTagsViewModel
            {
                AvailableTags = tags.Select(t => new TagCheckboxItem
                {
                    TagId = t.Id,
                    TagName = t.Name,
                    Selected = false
                }).ToList()
            };
        }

        public async Task<List<string>> MatchingUsersIds(string currentUserId, List<int> selectedTagIds, string? searchName = null, string? searchCity = null, string? searchCountry = null)
        {
            if (string.IsNullOrWhiteSpace(currentUserId)) return new List<string>();

            var connectedUserIds = await context.UserFriends
                .Where(f => f.UserId == currentUserId || f.FriendId == currentUserId)
                .Select(f => f.UserId == currentUserId ? f.FriendId : f.UserId)
                .ToListAsync();

            var query = context.Users.Where(u => u.Id != currentUserId && !connectedUserIds.Contains(u.Id));

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                query = query.Where(u => u.UserName != null && u.UserName.ToLower().Contains(searchName.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(searchCity))
            {
                query = query.Where(u => u.City != null && u.City.ToLower().Contains(searchCity.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(searchCountry))
            {
                query = query.Where(u => u.Country != null && u.Country.ToLower().Contains(searchCountry.ToLower()));
            }

            if (selectedTagIds != null && selectedTagIds.Any())
            {
                query = query.Where(u => u.UserTags.Any(ut => selectedTagIds.Contains(ut.TagId)));
            }

            var matchingUserIds = await query.Select(u => u.Id).ToListAsync();

            var random = new Random();
            return matchingUserIds.OrderBy(id => random.Next()).ToList();
        }

        public async Task<UserViewModel> MatchedUser(string targetUserId)
        {
            if (string.IsNullOrWhiteSpace(targetUserId)) return null;

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