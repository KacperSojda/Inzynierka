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