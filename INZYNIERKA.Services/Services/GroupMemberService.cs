using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services.Services
{
    public class GroupMemberService<TUser> : IGroupMemberService<TUser> where TUser : User
    {
        private readonly INZDbContext<TUser> context;

        public GroupMemberService(INZDbContext<TUser> context)
        {
            this.context = context;
        }

        public async Task<GroupMembersViewModel> GroupMembers(int groupId, string currentUserId)
        {
            var group = await context.Groups
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

        public async Task<bool> GiveAdmin(int groupId, string targetUserId, string currentUserId)
        {
            await EnsureIsAdminAsync(groupId, currentUserId);

            var userGroup = await context.UserGroups.FirstOrDefaultAsync(ug => ug.ChatGroupId == groupId && ug.UserId == targetUserId);
            if (userGroup == null) return false;

            if (userGroup.Type == MemberType.Banned) return false;

            if (userGroup.Type != MemberType.Administrator)
            {
                userGroup.Type = MemberType.Administrator;
                await context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> DemoteAdmin(int groupId, string targetUserId, string currentUserId)
        {
            await EnsureIsAdminAsync(groupId, currentUserId);
            if (targetUserId == currentUserId) throw new UnauthorizedAccessException("You cannot demote yourself.");

            var userGroup = await context.UserGroups.FirstOrDefaultAsync(ug => ug.ChatGroupId == groupId && ug.UserId == targetUserId);
            if (userGroup == null) return false;

            if (userGroup.Type != MemberType.Member)
            {
                userGroup.Type = MemberType.Member;
                await context.SaveChangesAsync();
            }
            return true;
        }

        public async Task<bool> KickUser(int groupId, string targetUserId, string currentUserId)
        {
            await EnsureIsAdminAsync(groupId, currentUserId);
            if (targetUserId == currentUserId) throw new UnauthorizedAccessException("You cannot kick yourself.");

            var userGroup = await context.UserGroups.FirstOrDefaultAsync(ug => ug.ChatGroupId == groupId && ug.UserId == targetUserId);
            if (userGroup == null) return false;

            context.UserGroups.Remove(userGroup);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> BanUser(int groupId, string targetUserId, string currentUserId)
        {
            await EnsureIsAdminAsync(groupId, currentUserId);
            if (targetUserId == currentUserId) throw new UnauthorizedAccessException("You cannot ban yourself.");

            var userGroup = await context.UserGroups.FirstOrDefaultAsync(ug => ug.ChatGroupId == groupId && ug.UserId == targetUserId);
            if (userGroup == null) return false;

            if (userGroup.Type != MemberType.Banned)
            {
                userGroup.Type = MemberType.Banned;
                await context.SaveChangesAsync();
            }
            return true;
        }

        private async Task EnsureIsAdminAsync(int groupId, string userId)
        {
            var isAdmin = await context.UserGroups
                .AnyAsync(ug => ug.ChatGroupId == groupId && ug.UserId == userId && ug.Type == MemberType.Administrator);

            if (!isAdmin)
            {
                throw new UnauthorizedAccessException("You do not have administrator privileges for this group.");
            }
        }
    }
}