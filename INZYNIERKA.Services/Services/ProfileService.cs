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
        private readonly INZDbContext<TUser> _context;
        private readonly UserManager<TUser> _userManager;

        public ProfileService(INZDbContext<TUser> context, UserManager<TUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>Retrieves profile details, including tags, for the current user.</summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A view model containing the user's profile information, or null if not found.</returns>
        public async Task<UserViewModel> Profile(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var user = await _context.Users
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

        /// <summary>Retrieves the user's profile data for editing interface.</summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A view model containing the user's editable profile information.</returns>
        public async Task<UserViewModel> EditProfile(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var user = await _context.Users.FindAsync(userId);
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

        /// <summary>Retrieves the public profile details of another user, hiding private information.</summary>
        /// <param name="targetUserId">The ID of the target user.</param>
        /// <returns>A view model containing the user's public profile information, or null if not found.</returns>
        public async Task<UserViewModel> OtherProfile(string targetUserId)
        {
            if (string.IsNullOrWhiteSpace(targetUserId)) return null;

            var user = await _context.Users
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

        /// <summary>Updates the user's profile information in the database.</summary>
        /// <param name="userId">The ID of the user being updated.</param>
        /// <param name="model">The view model containing the updated profile data.</param>
        /// <returns>A tuple containing the result and an ErrorMessage if the update fails.</returns>
        public async Task<(bool Result, string ErrorMessage)> UpdateProfile(string userId, UserViewModel model)
        {
            if (model == null) return (false, "No Data");

            var user = await _userManager.FindByIdAsync(userId);

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

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return (true, "");
            }

            return (false, "Failed to update profile");
        }

        /// <summary>Updates the user's avatar image.</summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="avatarData">The base64-encoded image data for the new avatar.</param>
        /// <returns>True if the avatar was updated successfully, otherwise false.</returns>
        public async Task<bool> UpdateAvatar(string userId, string avatarData)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(avatarData))
                return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            user.Avatar = avatarData;
            var result = await _userManager.UpdateAsync(user);

            return result.Succeeded;
        }

        /// <summary>Updates the user's profile cover image.</summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="coverData">The base64-encoded image data for the new cover.</param>
        /// <returns>True if the cover was updated successfully, otherwise false.</returns>
        public async Task<bool> UpdateCover(string userId, string coverData)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(coverData))
                return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            user.Cover = coverData;
            var result = await _userManager.UpdateAsync(user);

            return result.Succeeded;
        }
    }
}