using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services.Services
{
    public class TagService : ITagService
    {
        private readonly INZDbContext context;

        public TagService(INZDbContext context)
        {
            this.context = context;
        }

        public async Task<SelectTagsViewModel> GetUserTagsForSelectionAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return new SelectTagsViewModel { Tags = new List<TagItem>() };

            var userTagIds = await context.UserTags
                .Where(ut => ut.UserId == userId)
                .Select(ut => ut.TagId)
                .ToListAsync();

            var tags = await context.Tags.ToListAsync();

            return new SelectTagsViewModel
            {
                Tags = tags.Select(t => new TagItem
                {
                    TagId = t.Id,
                    TagName = t.Name,
                    IsSelected = userTagIds.Contains(t.Id)
                }).ToList()
            };
        }

        public async Task UpdateUserTagsAsync(string userId, List<int> selectedTagIds)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            var existingUserTags = await context.UserTags
                .Where(ut => ut.UserId == userId)
                .ToListAsync();

            var existingTagIds = existingUserTags.Select(ut => ut.TagId).ToList();

            var tagsToRemove = existingUserTags.Where(ut => !selectedTagIds.Contains(ut.TagId)).ToList();

            var tagsToAdd = selectedTagIds
                .Where(id => !existingTagIds.Contains(id))
                .Select(tagId => new UserTag { UserId = userId, TagId = tagId })
                .ToList();

            if (tagsToRemove.Any()) context.UserTags.RemoveRange(tagsToRemove);
            if (tagsToAdd.Any()) context.UserTags.AddRange(tagsToAdd);

            if (tagsToRemove.Any() || tagsToAdd.Any())
            {
                await context.SaveChangesAsync();
            }
        }

        public async Task AddNewTagAsync(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName)) return;

            var normalizedTagName = tagName.Trim();
            var searchName = normalizedTagName.ToLower();

            var tagExists = await context.Tags
                .AnyAsync(t => t.Name.ToLower() == searchName);

            if (!tagExists)
            {
                Tag tag = new Tag
                {
                    Name = normalizedTagName
                };

                context.Tags.Add(tag);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<Tag>> GetAllTagsAsync()
        {
            return await context.Tags.ToListAsync();
        }
    }
}