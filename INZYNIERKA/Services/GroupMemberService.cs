using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services
{
    public class GroupMemberService : IGroupMemberService
    {
        private readonly INZDbContext context;

        public GroupMemberService(INZDbContext context)
        {
            this.context = context;
        }

        public async Task<GroupMembersViewModel> GetGroupMembersAsync(int groupId, string currentUserId)
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
                Admins = group.Members.Where(m => m.Type == MemberType.Administrator).Select(m => new GroupMember { UserId = m.User.Id, Name = m.User.UserName }).ToList(),
                Members = group.Members.Where(m => m.Type == MemberType.Member).Select(m => new GroupMember { UserId = m.User.Id, Name = m.User.UserName }).ToList()
            };
        }

        public async Task<bool> GiveAdminAsync(int groupId, string targetUserId, string currentUserId)
        {
            await EnsureIsAdminAsync(groupId, currentUserId);

            var userGroup = await context.UserGroups.FirstOrDefaultAsync(ug => ug.ChatGroupId == groupId && ug.UserId == targetUserId);
            if (userGroup == null) return false;

            userGroup.Type = MemberType.Administrator;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DemoteAdminAsync(int groupId, string targetUserId, string currentUserId)
        {
            await EnsureIsAdminAsync(groupId, currentUserId);
            if (targetUserId == currentUserId) throw new UnauthorizedAccessException("Nie możesz zdegradować sam siebie.");

            var userGroup = await context.UserGroups.FirstOrDefaultAsync(ug => ug.ChatGroupId == groupId && ug.UserId == targetUserId);
            if (userGroup == null) return false;

            userGroup.Type = MemberType.Member;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> KickUserAsync(int groupId, string targetUserId, string currentUserId)
        {
            await EnsureIsAdminAsync(groupId, currentUserId);
            if (targetUserId == currentUserId) throw new UnauthorizedAccessException("Nie możesz wyrzucić sam siebie.");

            var userGroup = await context.UserGroups.FirstOrDefaultAsync(ug => ug.ChatGroupId == groupId && ug.UserId == targetUserId);
            if (userGroup == null) return false;

            context.UserGroups.Remove(userGroup);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> BanUserAsync(int groupId, string targetUserId, string currentUserId)
        {
            await EnsureIsAdminAsync(groupId, currentUserId);

            var userGroup = await context.UserGroups.FirstOrDefaultAsync(ug => ug.ChatGroupId == groupId && ug.UserId == targetUserId);
            if (userGroup == null) return false;

            userGroup.Type = MemberType.Banned;
            await context.SaveChangesAsync();
            return true;
        }

        private async Task EnsureIsAdminAsync(int groupId, string userId)
        {
            var isAdmin = await context.UserGroups
                .AnyAsync(ug => ug.ChatGroupId == groupId && ug.UserId == userId && ug.Type == MemberType.Administrator);

            if (!isAdmin)
            {
                throw new UnauthorizedAccessException("Brak uprawnień administratora grupy.");
            }
        }
    }
}