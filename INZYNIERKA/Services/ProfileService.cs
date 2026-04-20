using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services
{
    public class ProfileService : IProfileService
    {
        private readonly INZDbContext context;
        private readonly UserManager<User> userManager;

        public ProfileService(INZDbContext context, UserManager<User> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }

        public async Task<UserViewModel> GetUserProfileAsync(string userId)
        {
            var user = await context.Users.FindAsync(userId);
            if (user == null) return null;

            var userTags = await context.UserTags
                .Where(ut => ut.UserId == userId)
                .Include(ut => ut.Tag)
                .ToListAsync();

            return new UserViewModel
            {
                PrivateDescription = user.PrivateDescription,
                PublicDescription = user.PublicDescription,
                UserName = user.UserName,
                Avatar = user.Avatar,
                Tags = userTags.Select(ut => ut.Tag.Name).ToList(),
            };
        }

        public async Task<UserViewModel> GetUserProfileForEditAsync(string userId)
        {
            var user = await context.Users.FindAsync(userId);
            if (user == null) return null;

            return new UserViewModel
            {
                PrivateDescription = user.PrivateDescription,
                PublicDescription = user.PublicDescription,
                UserName = user.UserName,
                Avatar = user.Avatar,
            };
        }

        public async Task<UserViewModel> GetOtherUserProfileAsync(string targetUserId)
        {
            var user = await context.Users.FindAsync(targetUserId);
            if (user == null) return null;

            var userTags = await context.UserTags
                .Where(t => t.UserId == targetUserId)
                .Select(t => t.Tag.Name)
                .ToListAsync();

            return new UserViewModel
            {
                Id = targetUserId,
                Avatar = user.Avatar,
                UserName = user.UserName,
                PublicDescription = user.PublicDescription,
                PrivateDescription = "",
                Tags = userTags
            };
        }

        public async Task<(bool IsSuccess, IEnumerable<string> Errors)> UpdateUserProfileAsync(string userId, UserViewModel model)
        {
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return (false, new[] { "Nie znaleziono użytkownika." });
            }

            if (model.Avatar != null)
            {
                user.Avatar = model.Avatar;
            }

            user.PublicDescription = model.PublicDescription;
            user.PrivateDescription = model.PrivateDescription;

            var result = await userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return (true, Array.Empty<string>());
            }

            return (false, result.Errors.Select(e => e.Description));
        }
    }
}