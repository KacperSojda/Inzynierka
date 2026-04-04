using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services
{
    public class GroupService : IGroupService
    {
        private readonly INZDbContext context;

        public GroupService(INZDbContext context)
        {
            this.context = context;
        }

        public async Task<GroupViewModel> GetAvailableGroupsAsync(string userId)
        {
            var userGroupIds = await context.UserGroups
                .Where(ug => ug.UserId == userId)
                .Select(ug => ug.ChatGroupId)
                .ToListAsync();

            var model = await context.Groups
                .Include(g => g.GroupTags).ThenInclude(gt => gt.Tag)
                .Where(g => !userGroupIds.Contains(g.Id))
                .Select(g => new GroupItem
                {
                    GroupId = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    Tags = g.GroupTags.Select(gt => gt.Tag.Name).ToList()
                })
                .ToListAsync();

            return new GroupViewModel { Groups = model };
        }

        public async Task<GroupViewModel> GetUserGroupsAsync(string userId)
        {
            var userGroups = await context.UserGroups
                .Include(ug => ug.ChatGroup).ThenInclude(g => g.GroupTags).ThenInclude(gt => gt.Tag)
                .Where(ug => ug.UserId == userId)
                .ToListAsync();

            return new GroupViewModel
            {
                AdminGroups = userGroups.Where(ug => ug.Type == MemberType.Administrator).Select(MapToGroupItem).ToList(),
                Groups = userGroups.Where(ug => ug.Type == MemberType.Member).Select(MapToGroupItem).ToList()
            };
        }

        public async Task CreateGroupAsync(string name, string creatorUserId)
        {
            var group = new Group
            {
                Name = name,
                Description = "",
                Members = new List<UserGroup> { new UserGroup { UserId = creatorUserId, Type = MemberType.Administrator } }
            };

            context.Groups.Add(group);
            await context.SaveChangesAsync();
        }

        public async Task JoinGroupAsync(int groupId, string userId)
        {
            var alreadyMember = await context.UserGroups.AnyAsync(ug => ug.UserId == userId && ug.ChatGroupId == groupId);
            if (!alreadyMember)
            {
                context.UserGroups.Add(new UserGroup { UserId = userId, ChatGroupId = groupId, Type = MemberType.Member });
                await context.SaveChangesAsync();
            }
        }

        public async Task LeaveGroupAsync(int groupId, string userId)
        {
            var membership = await context.UserGroups.FirstOrDefaultAsync(ug => ug.UserId == userId && ug.ChatGroupId == groupId);
            if (membership != null)
            {
                context.UserGroups.Remove(membership);
                await context.SaveChangesAsync();
            }
        }

        public async Task<Group> GetGroupForEditAsync(int groupId, string currentUserId)
        {
            if (!await IsAdminAsync(groupId, currentUserId)) throw new UnauthorizedAccessException();
            return await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
        }

        public async Task UpdateGroupAsync(Group model, string currentUserId)
        {
            if (!await IsAdminAsync(model.Id, currentUserId)) throw new UnauthorizedAccessException();

            var group = await context.Groups.FindAsync(model.Id);
            if (group != null)
            {
                group.Name = model.Name;
                group.Description = model.Description;
                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteGroupAsync(int groupId, string currentUserId)
        {
            if (!await IsAdminAsync(groupId, currentUserId)) throw new UnauthorizedAccessException();

            var group = await context.Groups.Include(g => g.Members).Include(g => g.Messages).FirstOrDefaultAsync(g => g.Id == groupId);
            if (group != null)
            {
                context.UserGroups.RemoveRange(group.Members);
                if (group.Messages != null) context.GroupMessages.RemoveRange(group.Messages);
                context.Groups.Remove(group);
                await context.SaveChangesAsync();
            }
        }

        public async Task<SelectGroupTagsViewModel> GetGroupTagsForSelectionAsync(int groupId, string currentUserId)
        {
            if (!await IsAdminAsync(groupId, currentUserId)) throw new UnauthorizedAccessException();

            var groupTagIds = await context.GroupTags.Where(ut => ut.GroupId == groupId).Select(ut => ut.TagId).ToListAsync();
            var tags = await context.Tags.ToListAsync();

            return new SelectGroupTagsViewModel
            {
                GroupID = groupId,
                Tags = tags.Select(t => new TagItem { TagId = t.Id, TagName = t.Name, IsSelected = groupTagIds.Contains(t.Id) }).ToList()
            };
        }

        public async Task UpdateGroupTagsAsync(int groupId, string currentUserId, List<int> selectedTagIds)
        {
            if (!await IsAdminAsync(groupId, currentUserId)) throw new UnauthorizedAccessException();

            var existingGroupTags = await context.GroupTags.Where(ut => ut.GroupId == groupId).ToListAsync();
            context.GroupTags.RemoveRange(existingGroupTags);

            foreach (var tagId in selectedTagIds)
            {
                context.GroupTags.Add(new GroupTag { GroupId = groupId, TagId = tagId });
            }
            await context.SaveChangesAsync();
        }

        private async Task<bool> IsAdminAsync(int groupId, string userId)
        {
            return await context.UserGroups.AnyAsync(ug => ug.ChatGroupId == groupId && ug.UserId == userId && ug.Type == MemberType.Administrator);
        }

        private GroupItem MapToGroupItem(UserGroup ug)
        {
            return new GroupItem { GroupId = ug.ChatGroup.Id, Name = ug.ChatGroup.Name, Description = ug.ChatGroup.Description, Tags = ug.ChatGroup.GroupTags.Select(gt => gt.Tag.Name).ToList() };
        }
    }
}