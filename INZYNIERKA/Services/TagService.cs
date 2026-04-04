using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services
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
            var existingUserTags = await context.UserTags
                .Where(ut => ut.UserId == userId)
                .ToListAsync();

            context.UserTags.RemoveRange(existingUserTags);

            foreach (var tagId in selectedTagIds)
            {
                context.UserTags.Add(new UserTag
                {
                    UserId = userId,
                    TagId = tagId
                });
            }

            await context.SaveChangesAsync();
        }

        public async Task AddNewTagAsync(string tagName)
        {
            var tagExists = await context.Tags
                .AnyAsync(t => t.Name.ToLower() == tagName.ToLower());

            if (!tagExists)
            {
                Tag tag = new Tag
                {
                    Name = tagName
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