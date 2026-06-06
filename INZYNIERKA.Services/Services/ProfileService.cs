using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services.Services
{
    public class ProfileService<TUser> : IProfileService<TUser> where TUser : User
    {
        private readonly INZDbContext<TUser> context;
        private readonly UserManager<TUser> userManager;

        public ProfileService(INZDbContext<TUser> context, UserManager<TUser> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }

        public async Task<UserViewModel> Profile(string userId)
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
                Tags = user.UserTags.Select(ut => ut.Tag.Name).ToList(),
                BirthDate = user.BirthDate,
                Age = user.Age,
                City = user.City,
                Country = user.Country,
                Status = user.Status,
                Zodiac = user.Zodiac,
                Cover = user.Cover,
                Language = user.PreferredLanguages
            };
        }

        public async Task<UserViewModel> EditProfile(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var user = await context.Users.FindAsync(userId);
            if (user == null) return null;

            return new UserViewModel
            {
                PrivateDescription = user.PrivateDescription,
                PublicDescription = user.PublicDescription,
                UserName = user.UserName,
                Avatar = user.Avatar, 
                BirthDate = user.BirthDate,
                Age = user.Age,
                City = user.City,
                Country = user.Country,
                Status = user.Status,
                Zodiac = user.Zodiac,
                Cover = user.Cover,
                Language = user.PreferredLanguages
            };
        }

        public async Task<UserViewModel> OtherProfile(string targetUserId)
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
                Tags = user.UserTags.Select(ut => ut.Tag.Name).ToList(),
                BirthDate = user.BirthDate,
                Age = user.Age,
                City = user.City,
                Country = user.Country,
                Status = user.Status,
                Zodiac = user.Zodiac,
                Cover = user.Cover,
                Language = user.PreferredLanguages
            };
        }

        public async Task<(bool result, string ErrorMessage)> UpdateProfile(string userId, UserViewModel model)
        {
            if (model == null) return (false, "No Data");

            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return (false, "User not found");
            }

            user.Avatar = model.Avatar;
            user.PublicDescription = model.PublicDescription;
            user.PrivateDescription = model.PrivateDescription;
            if (model.BirthDate != null && model.BirthDate != default(DateTime))
            {
                user.BirthDate = DateTime.SpecifyKind(Convert.ToDateTime(model.BirthDate), DateTimeKind.Utc);
            }
            else
            {
                user.BirthDate = model.BirthDate;
            }
            user.City = model.City;
            user.Country = model.Country;
            user.Status = model.Status;
            user.Zodiac = model.Zodiac;
            user.PreferredLanguages = model.Language;

            var result = await userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return (true, "");
            }

            return (false, "Failed to update profile");
        }
        public async Task<bool> UpdateAvatar(string userId, string avatarData)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(avatarData))
                return false;

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            user.Avatar = avatarData;
            var result = await userManager.UpdateAsync(user);

            return result.Succeeded;
        }

        public async Task<bool> UpdateCover(string userId, string coverData)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(coverData))
                return false;

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            user.Cover = coverData;
            var result = await userManager.UpdateAsync(user);

            return result.Succeeded;
        }
    }
}