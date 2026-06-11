using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services.Services
{
    public class GroupMemberService<TUser> : IGroupMemberService<TUser> where TUser : User
    {
        private readonly INZDbContext<TUser> _context;
        private readonly UserManager<TUser> _userManager;
        private readonly SignInManager<TUser> _signInManager;


        public GroupMemberService(INZDbContext<TUser> context, UserManager<TUser> userManager, SignInManager<TUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>Retrieves a view model containing the administrators and regular members of a specific group.</summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="currentUserId">The ID of the current user requesting the data.</param>
        /// <returns>A populated GroupMembersViewModel.</returns>
        public async Task<GroupMembersViewModel> GroupMembers(int groupId, string currentUserId)
        {
            var isMember = await _context.UserGroups.AnyAsync(ug => ug.ChatGroupId == groupId && ug.UserId == currentUserId);

            if (!isMember) throw new UnauthorizedAccessException("You are not a member of this group.");

            var group = await _context.Groups
                .Include(g => g.Members)
                .ThenInclude(ug => ug.User)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return null;

            return new GroupMembersViewModel
            {
                GroupId = group.Id,
                Name = group.Name,
                CurrentUserId = currentUserId,
                Admins = group.Members.Where(m => m.Type == MemberType.Administrator).Select(m => new GroupMember {UserId = m.User.Id, Name = m.User.UserName}).ToList(),
                Members = group.Members.Where(m => m.Type == MemberType.Member).Select(m => new GroupMember {UserId = m.User.Id, Name = m.User.UserName}).ToList()
            };
        }

        /// <summary>Promotes a group member to an administrator role.</summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="targetUserId">The ID of the user to promote.</param>
        /// <param name="currentUserId">The ID of the current user performing the action.</param>
        /// <returns>True if the user was successfully promoted, otherwise false.</returns>
        public async Task<bool> GiveAdmin(int groupId, string targetUserId, string currentUserId)
        {
            await EnsureIsAdmin(groupId, currentUserId);

            var userGroup = await _context.UserGroups.FirstOrDefaultAsync(ug => ug.ChatGroupId == groupId && ug.UserId == targetUserId);

            if (userGroup == null) return false;

            if (userGroup.Type == MemberType.Banned) return false;

            if (userGroup.Type != MemberType.Administrator)
            {
                userGroup.Type = MemberType.Administrator;
                await _context.SaveChangesAsync();
            }

            return true;
        }

        /// <summary>Demotes a group administrator to a regular member role.</summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="targetUserId">The ID of the administrator to demote.</param>
        /// <param name="currentUserId">The ID of the current user performing the action.</param>
        /// <returns>True if the user was successfully demoted, otherwise false.</returns>
        public async Task<bool> DemoteAdmin(int groupId, string targetUserId, string currentUserId)
        {
            await EnsureIsAdmin(groupId, currentUserId);

            if (targetUserId == currentUserId) throw new UnauthorizedAccessException("You cannot demote yourself.");

            var userGroup = await _context.UserGroups.FirstOrDefaultAsync(ug => ug.ChatGroupId == groupId && ug.UserId == targetUserId);
            if (userGroup == null) return false;

            if (userGroup.Type != MemberType.Member)
            {
                userGroup.Type = MemberType.Member;
                await _context.SaveChangesAsync();
            }
            return true;
        }

        /// <summary>Removes a user from the group.</summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="targetUserId">The ID of the user to kick.</param>
        /// <param name="currentUserId">The ID of the current user performing the action.</param>
        /// <returns>True if the user was successfully removed, otherwise false.</returns>
        public async Task<bool> KickUser(int groupId, string targetUserId, string currentUserId)
        {
            await EnsureIsAdmin(groupId, currentUserId);

            if (targetUserId == currentUserId) throw new UnauthorizedAccessException("You cannot kick yourself.");

            var userGroup = await _context.UserGroups.FirstOrDefaultAsync(ug => ug.ChatGroupId == groupId && ug.UserId == targetUserId);
            if (userGroup == null) return false;

            _context.UserGroups.Remove(userGroup);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>Bans a user from the group, without removing their history.</summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="targetUserId">The ID of the user to ban.</param>
        /// <param name="currentUserId">The ID of the current user performing the action.</param>
        /// <returns>True if the user was successfully banned, otherwise false.</returns>
        public async Task<bool> BanUser(int groupId, string targetUserId, string currentUserId)
        {
            await EnsureIsAdmin(groupId, currentUserId);

            if (targetUserId == currentUserId) throw new UnauthorizedAccessException("You cannot ban yourself.");

            var userGroup = await _context.UserGroups.FirstOrDefaultAsync(ug => ug.ChatGroupId == groupId && ug.UserId == targetUserId);
            if (userGroup == null) return false;

            if (userGroup.Type != MemberType.Banned)
            {
                userGroup.Type = MemberType.Banned;
                await _context.SaveChangesAsync();
            }
            return true;
        }

        /// <summary>Retrieves a list of all currently banned users for a specific group.</summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <returns>List of banned users.</returns>
        public async Task<BannedMembersViewModel> GetBannedUsers(int groupId)
        {
            var groupName = await _context.Groups
                .Where(g => g.Id == groupId)
                .Select(g => g.Name)
                .FirstOrDefaultAsync() ?? "Unknown Group";

            var bannedUsers = await _context.UserGroups
                .Include(ug => ug.User)
                .Where(ug => ug.ChatGroupId == groupId && ug.Type == MemberType.Banned)
                .Select(ug => new BannedUserDto
                {
                    UserId = ug.UserId,
                    UserName = ug.User.UserName
                })
                .ToListAsync();

            return new BannedMembersViewModel
            {
                GroupId = groupId,
                GroupName = groupName,
                BannedUsers = bannedUsers
            };
        }

        /// <summary>Removes the ban on a user, restoring their status to a regular group member.</summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="userId">The ID of the user to unban.</param>
        public async Task UnbanUser(int groupId, string userId)
        {
            var userGroup = await _context.UserGroups
                .FirstOrDefaultAsync(ug => ug.ChatGroupId == groupId && ug.UserId == userId);

            if (userGroup != null && userGroup.Type == MemberType.Banned)
            {
                userGroup.Type = MemberType.Member;
                await _context.SaveChangesAsync();
            }
        }

        private async Task EnsureIsAdmin(int groupId, string userId)
        {
            var isAdmin = await _context.UserGroups
                .AnyAsync(ug => ug.ChatGroupId == groupId && ug.UserId == userId && ug.Type == MemberType.Administrator);

            if (!isAdmin)
            {
                throw new UnauthorizedAccessException("You do not have administrator privileges for this group.");
            }
        }
    }
}