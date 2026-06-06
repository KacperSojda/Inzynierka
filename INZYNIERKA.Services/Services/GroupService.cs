using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services.Services
{
    public class GroupService<TUser> : IGroupService<TUser> where TUser : User
    {
        private readonly INZDbContext<TUser> _context;

        public GroupService(INZDbContext<TUser> context)
        {
            _context = context;
        }

        public async Task<(GroupViewModel Model, int TotalCount)> AvailableGroups(string userId, string? searchQuery = null, int page = 1, int pageSize = 10)
        {
            var userGroupIds = await _context.UserGroups
                .Where(ug => ug.UserId == userId)
                .Select(ug => ug.ChatGroupId)
                .ToListAsync();

            var availableGroupsQuery = _context.Groups
                .Include(g => g.GroupTags).ThenInclude(gt => gt.Tag)
                .Where(g => !userGroupIds.Contains(g.Id));

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var lowerSearchQuery = searchQuery.ToLower();
                availableGroupsQuery = availableGroupsQuery.Where(g => g.Name.ToLower().Contains(lowerSearchQuery));
            }

            int totalCount = await availableGroupsQuery.CountAsync();

            var model = await availableGroupsQuery
                .OrderBy(g => g.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(g => new GroupItem
                {
                    GroupId = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    Tags = g.GroupTags.Select(gt => gt.Tag.Name).ToList()
                })
                .ToListAsync();

            return (new GroupViewModel { Groups = model }, totalCount);
        }

        public async Task<(GroupViewModel Model, int TotalCount)> UserGroups(string userId, string? searchQuery = null, int page = 1, int pageSize = 10)
        {
            var query = _context.UserGroups
                .Include(ug => ug.ChatGroup).ThenInclude(g => g.GroupTags).ThenInclude(gt => gt.Tag)
                .Where(ug => ug.UserId == userId);

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var lowerSearchQuery = searchQuery.ToLower();
                query = query.Where(ug => ug.ChatGroup.Name.ToLower().Contains(lowerSearchQuery));
            }

            int totalCount = await query.CountAsync();

            var userGroups = await query
                .OrderBy(ug => ug.ChatGroup.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new GroupViewModel
            {
                Groups = userGroups.Select(MapToGroupItem).ToList()
            };

            return (model, totalCount);
        }

        public async Task CreateGroup(string name, string creatorUserId)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Group name cannot be empty.");

            var group = new Group
            {
                Name = name,
                Description = "",
                Members = new List<UserGroup> { new UserGroup { UserId = creatorUserId, Type = MemberType.Administrator } }
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();
        }

        public async Task JoinGroup(int groupId, string userId)
        {
            var alreadyMember = await _context.UserGroups.FirstOrDefaultAsync(ug => ug.UserId == userId && ug.ChatGroupId == groupId);

            if (alreadyMember == null)
            {
                _context.UserGroups.Add(new UserGroup { UserId = userId, ChatGroupId = groupId, Type = MemberType.Member });
                await _context.SaveChangesAsync();
            }
        }

        public async Task LeaveGroup(int groupId, string userId)
        {
            var membership = await _context.UserGroups.FirstOrDefaultAsync(ug => ug.UserId == userId && ug.ChatGroupId == groupId);

            if (membership == null) return;

            if (membership.Type == MemberType.Administrator)
            {
                var adminCount = await _context.UserGroups.CountAsync(ug => ug.ChatGroupId == groupId && ug.Type == MemberType.Administrator);
                if (adminCount <= 1)
                {
                    throw new InvalidOperationException("You cannot leave the group as the last administrator. Please transfer admin rights or delete the group.");
                }
            }

            _context.UserGroups.Remove(membership);
            await _context.SaveChangesAsync();
        }

        public async Task<EditGroupViewModel> EditGroup(int groupId, string userId)
        {
            if (!await IsAdminAsync(groupId, userId)) throw new UnauthorizedAccessException();

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return null;

            return new EditGroupViewModel
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description
            };
        }

        public async Task UpdateGroup(EditGroupViewModel model, string userId)    
        {
            if (model == null || model.Id <= 0) return;
            if (!await IsAdminAsync(model.Id, userId)) throw new UnauthorizedAccessException();

            var group = await _context.Groups.FindAsync(model.Id);
            if (group != null)
            {
                group.Name = model.Name;
                group.Description = model.Description;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteGroup(int groupId, string currentUserId)
        {
            if (!await IsAdminAsync(groupId, currentUserId)) throw new UnauthorizedAccessException();

            var group = await _context.Groups.Include(g => g.Members).Include(g => g.Messages).FirstOrDefaultAsync(g => g.Id == groupId);

            if (group != null)
            {
                _context.Groups.Remove(group);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<SelectGroupTagsViewModel> GroupTags(int groupId, string currentUserId)
        {
            if (!await IsAdminAsync(groupId, currentUserId)) throw new UnauthorizedAccessException();

            var groupTagIds = await _context.GroupTags.Where(ut => ut.GroupId == groupId).Select(ut => ut.TagId).ToListAsync();
            var tags = await _context.Tags.ToListAsync();

            return new SelectGroupTagsViewModel
            {
                GroupID = groupId,
                Tags = tags.Select(t => new TagItem { TagId = t.Id, TagName = t.Name, Selected = groupTagIds.Contains(t.Id) }).ToList()
            };
        }

        public async Task UpdateGroupTags(int groupId, string currentUserId, List<int> selectedTagIds)
        {
            if (!await IsAdminAsync(groupId, currentUserId)) throw new UnauthorizedAccessException();

            var existingGroupTags = await _context.GroupTags.Where(ut => ut.GroupId == groupId).ToListAsync();
            _context.GroupTags.RemoveRange(existingGroupTags);

            foreach (var tagId in selectedTagIds)
            {
                _context.GroupTags.Add(new GroupTag { GroupId = groupId, TagId = tagId });
            }
            await _context.SaveChangesAsync();
        }

        private async Task<bool> IsAdminAsync(int groupId, string userId)
        {
            return await _context.UserGroups.AnyAsync(ug => ug.ChatGroupId == groupId && ug.UserId == userId && ug.Type == MemberType.Administrator);
        }

        private GroupItem MapToGroupItem(UserGroup ug)
        {
            return new GroupItem { 
                GroupId = ug.ChatGroup.Id, 
                Name = ug.ChatGroup.Name, 
                Description = ug.ChatGroup.Description, 
                Tags = ug.ChatGroup.GroupTags.Select(gt => gt.Tag.Name).ToList(),
                IsAdmin = ug.Type == MemberType.Administrator
            };
        }
    }
}