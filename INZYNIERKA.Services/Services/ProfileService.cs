using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services.Services
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
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var user = await context.Users
                .Include(u => u.UserTags)
                    .ThenInclude(ut => ut.Tag)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return null;

            return new UserViewModel
            {
                PrivateDescription = user.PrivateDescription,
                PublicDescription = user.PublicDescription,
                UserName = user.UserName,
                Avatar = user.Avatar,
                Tags = user.UserTags.Select(ut => ut.Tag.Name).ToList()
            };
        }

        public async Task<UserViewModel> GetUserProfileForEditAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var user = await context.Users.FindAsync(userId);
            if (user == null) return null;

            return new UserViewModel
            {
                PrivateDescription = user.PrivateDescription,
                PublicDescription = user.PublicDescription,
                UserName = user.UserName,
                Avatar = user.Avatar
            };
        }

        public async Task<UserViewModel> GetOtherUserProfileAsync(string targetUserId)
        {
            if (string.IsNullOrWhiteSpace(targetUserId)) return null;

            var user = await context.Users
                .Include(u => u.UserTags)
                    .ThenInclude(ut => ut.Tag)
                .FirstOrDefaultAsync(u => u.Id == targetUserId);

            if (user == null) return null;

            return new UserViewModel
            {
                Id = targetUserId,
                Avatar = user.Avatar,
                UserName = user.UserName,
                PublicDescription = user.PublicDescription,
                PrivateDescription = "",
                Tags = user.UserTags.Select(ut => ut.Tag.Name).ToList()
            };
        }

        public async Task<(bool IsSuccess, IEnumerable<string> Errors)> UpdateUserProfileAsync(string userId, UserViewModel model)
        {
            if (string.IsNullOrWhiteSpace(userId)) return (false, new[] {"Authorization error."});
            if (model == null) return (false, new[] {"Empty data sended"});

            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return (false, new[] { "User not found" });
            }

            if (model.Avatar != null)
            {
                user.Avatar = model.Avatar;
            }

            user.PublicDescription = model.PublicDescription;
            user.PrivateDescription = model.PrivateDescription;
            user.DateOfBirth = model.DateOfBirth;
            user.City = model.City;
            user.Country = model.Country;
            user.CustomStatus = model.CustomStatus;
            user.Zodiac = model.Zodiac;
            user.SocialMediaUrl = model.SocialMediaUrl;
            user.PreferredLanguages = model.PreferredLanguages;

            var result = await userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return (true, Array.Empty<string>());
            }

            return (false, result.Errors.Select(e => e.Description));
        }
    }
}