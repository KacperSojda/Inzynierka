using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services.Services
{
    public class TagService<TUser> : ITagService<TUser> where TUser : User
    {
        private readonly INZDbContext<TUser> _context;

        public TagService(INZDbContext<TUser> context)
        {
            _context = context;
        }

        /// <summary>Retrieves all available tags, indicating which ones are currently assigned to the user.</summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A view model containing the tag selection state for the user.</returns>
        public async Task<SelectTagsViewModel> UserTags(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return new SelectTagsViewModel { Tags = new List<TagItem>() };

            var userTagIds = await _context.UserTags
                .Where(ut => ut.UserId == userId)
                .Select(ut => ut.TagId)
                .ToListAsync();

            var tags = await _context.Tags.ToListAsync();

            return new SelectTagsViewModel
            {
                Tags = tags.Select(t => new TagItem
                {
                    TagId = t.Id,
                    TagName = t.Name,
                    Selected = userTagIds.Contains(t.Id)
                }).ToList()
            };
        }

        /// <summary>Updates the tags associated with a user.</summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="selectedTagIds">A list of tag IDs to be assigned to the user.</param>
        public async Task UpdateUserTags(string userId, List<int> selectedTagIds)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            var existingUserTags = await _context.UserTags
                .Where(ut => ut.UserId == userId)
                .ToListAsync();

            var existingTagIds = existingUserTags.Select(ut => ut.TagId).ToList();

            var tagsToRemove = existingUserTags.Where(ut => !selectedTagIds.Contains(ut.TagId)).ToList();

            var tagsToAdd = selectedTagIds
                .Where(id => !existingTagIds.Contains(id))
                .Select(tagId => new UserTag { UserId = userId, TagId = tagId })
                .ToList();

            if (tagsToRemove.Any()) _context.UserTags.RemoveRange(tagsToRemove);
            if (tagsToAdd.Any()) _context.UserTags.AddRange(tagsToAdd);

            if (tagsToRemove.Any() || tagsToAdd.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>Creates a new tag if one with the same name does not already exist.</summary>
        /// <param name="tagName">The name of the new tag.</param>
        /// <returns>A tuple containing the result and an ErrorMessage if the creation fails.</returns>
        public async Task<(bool Result, string ErrorMessage)> NewTag(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName)) return (false, "Tag name cannot be empty");

            var normalizedTagName = tagName.Trim();
            var searchName = normalizedTagName.ToLower();

            var tagExists = await _context.Tags
                .AnyAsync(t => t.Name.ToLower() == searchName);

            if (tagExists)
            {
                return (false, "Tag already exists");
            }

            Tag tag = new Tag
            {
                Name = normalizedTagName
            };

            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();
            return (true, "");
        }

        /// <summary>Retrieves a complete list of all tags stored in the database.</summary>
        /// <returns>A list of all tags.</returns>
        public async Task<List<Tag>> AllTags()
        {
            return await _context.Tags.ToListAsync();
        }
    }
}