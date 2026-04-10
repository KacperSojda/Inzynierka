using System;
using System.Linq;
using System.Threading.Tasks;
using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace INZYNIERKA.Tests.Services
{
    public class GroupMemberServiceTests
    {
        private INZDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<INZDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new INZDbContext(options);
        }

        // TESTY DLA: GetGroupMembersAsync //

        [Fact]
        public async Task GetGroupMembersAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupMemberService(context);
            var groupId = 1;

            var group = new Group {Id = groupId, Name = "Grupa Testowa", Description = ""};
            var adminUser = new User {Id = "admin", UserName = "Admin", Avatar = "", PublicDescription = "", PrivateDescription = "" };
            var memberUser = new User {Id = "member", UserName = "Członek", Avatar = "", PublicDescription = "", PrivateDescription = "" };

            context.Groups.Add(group);
            context.Users.AddRange(adminUser, memberUser);

            context.UserGroups.AddRange(
                new UserGroup {ChatGroupId = groupId, UserId = adminUser.Id, Type = MemberType.Administrator, User = adminUser},
                new UserGroup {ChatGroupId = groupId, UserId = memberUser.Id, Type = MemberType.Member, User = memberUser}
            );
            await context.SaveChangesAsync();

            var result = await service.GetGroupMembersAsync(groupId, "admin");

            Assert.NotNull(result);
            Assert.Single(result.Admins);
            Assert.Equal("admin", result.Admins.First().UserId);
            Assert.Single(result.Members);
            Assert.Equal("member", result.Members.First().UserId);
        }

        // TESTY DLA: EnsureIsAdminAsync (Ochrona wszystkich metod) //

        [Fact]
        public async Task GiveAdminAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupMemberService(context);
            var groupId = 1;
            var adminId = "Admin";
            var friendId = "Znajomy";

            // Dodajemy użytkownika jako zwykłego członka (nie administratora)
            context.UserGroups.Add(new UserGroup {ChatGroupId = groupId, UserId = adminId, Type = MemberType.Member});
            await context.SaveChangesAsync();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GiveAdminAsync(groupId, friendId, adminId));
        }

        // TESTY DLA: Akcje na członkach grupy //

        [Fact]
        public async Task GiveAdminAsyncTest2()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupMemberService(context);
            var groupId = 1;
            var adminId = "Admin";
            var memberId = "Member";

            context.UserGroups.Add(new UserGroup {ChatGroupId = groupId, UserId = adminId, Type = MemberType.Administrator});
            context.UserGroups.Add(new UserGroup {ChatGroupId = groupId, UserId = memberId, Type = MemberType.Member});
            await context.SaveChangesAsync();

            var result = await service.GiveAdminAsync(groupId, memberId, adminId);

            var promotedUser = await context.UserGroups.FirstAsync(ug => ug.UserId == memberId);
            Assert.True(result);
            Assert.Equal(MemberType.Administrator, promotedUser.Type);
        }

        [Fact]
        public async Task DemoteAdminAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupMemberService(context);
            var groupId = 1;
            var adminId = "Admin";

            context.UserGroups.Add(new UserGroup {ChatGroupId = groupId, UserId = adminId, Type = MemberType.Administrator});
            await context.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.DemoteAdminAsync(groupId, adminId, adminId));

            Assert.Equal("Nie możesz zdegradować sam siebie.", exception.Message);
        }

        [Fact]
        public async Task KickUserAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupMemberService(context);
            var groupId = 1;
            var adminId = "Admin";

            context.UserGroups.Add(new UserGroup {ChatGroupId = groupId, UserId = adminId, Type = MemberType.Administrator});
            await context.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.KickUserAsync(groupId, adminId, adminId));

            Assert.Equal("Nie możesz wyrzucić sam siebie.", exception.Message);
        }

        [Fact]
        public async Task KickUserAsyncTest2()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupMemberService(context);
            var groupId = 1;
            var adminId = "Admin";
            var memberId = "Member";

            context.UserGroups.Add(new UserGroup {ChatGroupId = groupId, UserId = adminId, Type = MemberType.Administrator});
            context.UserGroups.Add(new UserGroup {ChatGroupId = groupId, UserId = memberId, Type = MemberType.Member});
            await context.SaveChangesAsync();

            var result = await service.KickUserAsync(groupId, memberId, adminId);

            var isStillInGroup = await context.UserGroups.AnyAsync(ug => ug.UserId == memberId);
            Assert.True(result);
            Assert.False(isStillInGroup);
        }

        [Fact]
        public async Task BanUserAsynctEST()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupMemberService(context);
            var groupId = 1;
            var adminId = "Admin";
            var memberId = "Member";

            context.UserGroups.Add(new UserGroup {ChatGroupId = groupId, UserId = adminId, Type = MemberType.Administrator});
            context.UserGroups.Add(new UserGroup {ChatGroupId = groupId, UserId = memberId, Type = MemberType.Member});
            await context.SaveChangesAsync();

            var result = await service.BanUserAsync(groupId, memberId, adminId);

            var bannedUser = await context.UserGroups.FirstAsync(ug => ug.UserId == memberId);
            Assert.True(result);
            Assert.Equal(MemberType.Banned, bannedUser.Type);
        }
    }
}