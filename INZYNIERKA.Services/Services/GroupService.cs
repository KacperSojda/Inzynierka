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

        /// <summary>Retrieves a paginated and filtered list of groups the user is not currently a member.</summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="searchQuery">An optional search string to filter groups by name.</param>
        /// <param name="page">The page number for pagination.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A tuple containing the list of available groups and the total count of matches.</returns>
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

        /// <summary>Retrieves a paginated and filtered list of groups the user is currently a member.</summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="searchQuery">An optional search string to filter groups by name.</param>
        /// <param name="page">The page number for pagination.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A tuple containing the list of the user's groups and the total count.</returns>
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

        /// <summary>Creates a new chat group and assigns user as its initial administrator.</summary>
        /// <param name="name">The name of the new group.</param>
        /// <param name="creatorUserId">The ID of the user creating the group.</param>
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

        /// <summary>Adds a user to a specific group as a regular member.</summary>
        /// <param name="groupId">The ID of the group to join.</param>
        /// <param name="userId">The ID of the user joining the group.</param>
        public async Task JoinGroup(int groupId, string userId)
        {
            var alreadyMember = await _context.UserGroups.FirstOrDefaultAsync(ug => ug.UserId == userId && ug.ChatGroupId == groupId);

            if (alreadyMember == null)
            {
                _context.UserGroups.Add(new UserGroup { UserId = userId, ChatGroupId = groupId, Type = MemberType.Member });
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>Removes a user from a group, ensuring that the last administrator cannot leave without transferring rights or deleting the group.</summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="userId">The ID of the user leaving the group.</param>
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

        /// <summary>Retrieves the details of a group for editing purposes, verifying administrator privileges.</summary>
        /// <param name="groupId">The ID of the group to edit.</param>
        /// <param name="userId">The ID of the user requesting the edit.</param>
        /// <returns>A view model containing the group's current name and description.</returns>
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

        /// <summary>Updates the name and description of an existing group, verifying administrator privileges.</summary>
        /// <param name="model">The view model containing the updated group details.</param>
        /// <param name="userId">The ID of the user performing the update.</param>
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

        /// <summary>Deletes a group and its associated data entirely, verifying administrator privileges.</summary>
        /// <param name="groupId">The ID of the group to delete.</param>
        /// <param name="currentUserId">The ID of the user performing the deletion.</param>
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

        /// <summary>Retrieves all available tags, indicating which ones are currently assigned to the group.</summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="currentUserId">The ID of the user requesting the tags.</param>
        /// <returns>A view model containing the tag selection state.</returns>
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

        /// <summary>Updates the tags associated with a specific group.</summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="currentUserId">The ID of the user updating the tags.</param>
        /// <param name="selectedTagIds">A list of tag IDs to be assigned to the group.</param>
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