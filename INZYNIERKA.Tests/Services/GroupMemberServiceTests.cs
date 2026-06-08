using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Tests.Services
{
    public class GroupMemberServiceTests
    {
        private INZDbContext<User> CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<INZDbContext<User>>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new INZDbContext<User>(options);
        }

        // TESTS FOR: GroupMembers //

        [Fact]
        public async Task GroupMembers_ReturnsAdminsAndMembers()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupMemberService<User>(context, null, null);
            var groupId = 1;

            var group = new Group { Id = groupId, Name = "Test Group", Description = "" };
            var adminUser = new User { Id = "admin", UserName = "Admin", Avatar = "", PublicDescription = "", PrivateDescription = "" };
            var memberUser = new User { Id = "member", UserName = "Member", Avatar = "", PublicDescription = "", PrivateDescription = "" };

            context.Groups.Add(group);
            context.Users.AddRange(adminUser, memberUser);

            context.UserGroups.AddRange(
                new UserGroup { ChatGroupId = groupId, UserId = adminUser.Id, Type = MemberType.Administrator, User = adminUser },
                new UserGroup { ChatGroupId = groupId, UserId = memberUser.Id, Type = MemberType.Member, User = memberUser }
            );
            await context.SaveChangesAsync();

            var result = await service.GroupMembers(groupId, "admin");

            Assert.NotNull(result);
            Assert.Single(result.Admins);
            Assert.Equal("admin", result.Admins.First().UserId);
            Assert.Single(result.Members);
            Assert.Equal("member", result.Members.First().UserId);
        }

        [Fact]
        public async Task GroupMembers_ThrowsUnauthorized()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupMemberService<User>(context, null, null);
            var groupId = 1;

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GroupMembers(groupId, "nonMemberId"));

            Assert.Equal("You are not a member of this group.", exception.Message);
        }

        // TESTS FOR: GiveAdmin //

        [Fact]
        public async Task GiveAdmin_ThrowsUnauthorized()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupMemberService<User>(context, null, null);
            var groupId = 1;
            var nonAdminId = "Caller";
            var targetId = "Target";

            context.UserGroups.Add(new UserGroup { ChatGroupId = groupId, UserId = nonAdminId, Type = MemberType.Member });
            await context.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GiveAdmin(groupId, targetId, nonAdminId));

            Assert.Equal("You do not have administrator privileges for this group.", exception.Message);
        }

        [Fact]
        public async Task GiveAdmin_PromotesUserToAdmin()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupMemberService<User>(context, null, null);
            var groupId = 1;
            var adminId = "Admin";
            var memberId = "Member";

            context.UserGroups.Add(new UserGroup { ChatGroupId = groupId, UserId = adminId, Type = MemberType.Administrator });
            context.UserGroups.Add(new UserGroup { ChatGroupId = groupId, UserId = memberId, Type = MemberType.Member });
            await context.SaveChangesAsync();

            var result = await service.GiveAdmin(groupId, memberId, adminId);

            var promotedUser = await context.UserGroups.FirstAsync(ug => ug.UserId == memberId);
            Assert.True(result);
            Assert.Equal(MemberType.Administrator, promotedUser.Type);
        }

        // TESTS FOR: DemoteAdmin //

        [Fact]
        public async Task DemoteAdmin_ThrowsUnauthorized()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupMemberService<User>(context, null, null);
            var groupId = 1;
            var adminId = "Admin";

            context.UserGroups.Add(new UserGroup { ChatGroupId = groupId, UserId = adminId, Type = MemberType.Administrator });
            await context.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.DemoteAdmin(groupId, adminId, adminId));

            Assert.Equal("You cannot demote yourself.", exception.Message);
        }

        // TESTS FOR: KickUser //

        [Fact]
        public async Task KickUser_ThrowsUnauthorized()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupMemberService<User>(context, null, null);
            var groupId = 1;
            var adminId = "Admin";

            context.UserGroups.Add(new UserGroup { ChatGroupId = groupId, UserId = adminId, Type = MemberType.Administrator });
            await context.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.KickUser(groupId, adminId, adminId));

            Assert.Equal("You cannot kick yourself.", exception.Message);
        }

        [Fact]
        public async Task KickUser_RemovesUserFromGroup()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupMemberService<User>(context, null, null);
            var groupId = 1;
            var adminId = "Admin";
            var memberId = "Member";

            context.UserGroups.Add(new UserGroup { ChatGroupId = groupId, UserId = adminId, Type = MemberType.Administrator });
            context.UserGroups.Add(new UserGroup { ChatGroupId = groupId, UserId = memberId, Type = MemberType.Member });
            await context.SaveChangesAsync();

            var result = await service.KickUser(groupId, memberId, adminId);

            var isStillInGroup = await context.UserGroups.AnyAsync(ug => ug.UserId == memberId);
            Assert.True(result);
            Assert.False(isStillInGroup);
        }

        // TESTS FOR: BanUser //

        [Fact]
        public async Task BanUser_SetsMemberTypeToBanned()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupMemberService<User>(context, null, null);
            var groupId = 1;
            var adminId = "Admin";
            var memberId = "Member";

            context.UserGroups.Add(new UserGroup { ChatGroupId = groupId, UserId = adminId, Type = MemberType.Administrator });
            context.UserGroups.Add(new UserGroup { ChatGroupId = groupId, UserId = memberId, Type = MemberType.Member });
            await context.SaveChangesAsync();

            var result = await service.BanUser(groupId, memberId, adminId);

            var bannedUser = await context.UserGroups.FirstAsync(ug => ug.UserId == memberId);
            Assert.True(result);
            Assert.Equal(MemberType.Banned, bannedUser.Type);
        }

        // TESTS FOR: GetBannedUsers //

        [Fact]
        public async Task GetBannedUsers_ReturnsBannedUsers()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupMemberService<User>(context, null, null);
            var groupId = 1;

            var group = new Group { Id = groupId, Name = "Test Group", Description = "Test Description" };
            var bannedUser = new User { Id = "bannedUser", UserName = "Bad Guy", PrivateDescription = "PrivateDescription", PublicDescription = "PublicDescription", Avatar = "DefaultAvatar" };

            context.Groups.Add(group);
            context.Users.Add(bannedUser);
            context.UserGroups.Add(new UserGroup { ChatGroupId = groupId, UserId = bannedUser.Id, Type = MemberType.Banned, User = bannedUser });
            await context.SaveChangesAsync();

            var result = await service.GetBannedUsers(groupId);

            Assert.NotNull(result);
            Assert.Equal(groupId, result.GroupId);
            Assert.Equal("Test Group", result.GroupName);
            Assert.Single(result.BannedUsers);
            Assert.Equal("bannedUser", result.BannedUsers.First().UserId);
        }

        // TESTS FOR: UnbanUser //

        [Fact]
        public async Task UnbanUser_SetsMemberTypeToMember()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupMemberService<User>(context, null, null);
            var groupId = 1;
            var userId = "bannedUser";

            context.UserGroups.Add(new UserGroup { ChatGroupId = groupId, UserId = userId, Type = MemberType.Banned });
            await context.SaveChangesAsync();

            await service.UnbanUser(groupId, userId);

            var userGroup = await context.UserGroups.FirstAsync(ug => ug.UserId == userId);
            Assert.Equal(MemberType.Member, userGroup.Type);
        }
    }
}