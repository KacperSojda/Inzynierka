using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Tests.Services
{
    public class GroupServiceTests
    {
        private INZDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<INZDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new INZDbContext(options);
        }

        // TESTY DLA: GetAvailableGroupsAsync //

        [Fact]
        public async Task GetAvailableGroupsAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var userId = "Ja";

            var groupJoined = new Group {Id = 1, Name = "Moja Grupa", Description = ""};
            var groupAvailable = new Group {Id = 2, Name = "Nowa Grupa", Description = ""};

            context.Groups.AddRange(groupJoined, groupAvailable);
            context.UserGroups.Add(new UserGroup {UserId = userId, ChatGroupId = 1, Type = MemberType.Member});

            await context.SaveChangesAsync();
            var service = new GroupService(context);

            var result = await service.GetAvailableGroupsAsync(userId);

            Assert.NotNull(result);
            Assert.Single(result.Groups);
            Assert.Equal(2, result.Groups.First().GroupId);
        }

        // TESTY DLA: GetUserGroupsAsync //

        [Fact]
        public async Task GetUserGroupsAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var userId = "Ja";

            var adminGroup = new Group {Id = 1, Name = "Grupa Admina", Description = ""};
            var memberGroup = new Group {Id = 2, Name = "Grupa Członka", Description = ""};

            context.Groups.AddRange(adminGroup, memberGroup);
            context.UserGroups.AddRange(
                new UserGroup {UserId = userId, ChatGroupId = 1, Type = MemberType.Administrator, ChatGroup = adminGroup},
                new UserGroup {UserId = userId, ChatGroupId = 2, Type = MemberType.Member, ChatGroup = memberGroup}
            );

            await context.SaveChangesAsync();
            var service = new GroupService(context);

            var result = await service.GetUserGroupsAsync(userId);

            Assert.NotNull(result);
            Assert.Single(result.AdminGroups);
            Assert.Equal(1, result.AdminGroups.First().GroupId);
            Assert.Single(result.Groups);
            Assert.Equal(2, result.Groups.First().GroupId);
        }

        // TESTY DLA: CreateGroupAsync //

        [Fact]
        public async Task CreateGroupAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupService(context);
            var userId = "Ja";

            await service.CreateGroupAsync("Moja Grupa", userId);

            var group = await context.Groups.Include(g => g.Members).FirstOrDefaultAsync();

            Assert.NotNull(group);
            Assert.Equal("Moja Grupa", group.Name);
            Assert.Single(group.Members);
            Assert.Equal(userId, group.Members.First().UserId);
            Assert.Equal(MemberType.Administrator, group.Members.First().Type);
        }

        // TESTY DLA: JoinGroupAsync //

        [Fact]
        public async Task JoinGroupAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupService(context);
            var groupId = 1;
            var userId = "Ja";

            context.Groups.Add(new Group {Id = groupId, Name = "Testowa Grupa", Description = ""});
            await context.SaveChangesAsync();

            await service.JoinGroupAsync(groupId, userId);

            var membership = await context.UserGroups.FirstOrDefaultAsync(ug => ug.UserId == userId && ug.ChatGroupId == groupId);
            Assert.NotNull(membership);
            Assert.Equal(MemberType.Member, membership.Type);
        }

        // TESTY DLA: LeaveGroupAsync //

        [Fact]
        public async Task LeaveGroupAsync_RemovesMembership()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupService(context);
            var groupId = 1;
            var userId = "Ja";

            context.UserGroups.Add(new UserGroup {UserId = userId, ChatGroupId = groupId, Type = MemberType.Member});
            await context.SaveChangesAsync();

            await service.LeaveGroupAsync(groupId, userId);

            var membership = await context.UserGroups.FirstOrDefaultAsync(ug => ug.UserId == userId && ug.ChatGroupId == groupId);
            Assert.Null(membership);
        }

        // TESTY AUTORYZACJI (IsAdminAsync) //

        [Fact]
        public async Task DeleteGroupAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupService(context);
            var groupId = 1;
            var userId = "Ja";

            context.Groups.Add(new Group {Id = groupId, Name = "Testowa Grupa", Description = ""});
            context.UserGroups.Add(new UserGroup {UserId = userId, ChatGroupId = groupId, Type = MemberType.Member});
            await context.SaveChangesAsync();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteGroupAsync(groupId, userId));
        }

        [Fact]
        public async Task UpdateGroupAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupService(context);
            var groupId = 1;
            var userId = "Ja";

            context.Groups.Add(new Group {Id = groupId, Name = "Stara Nazwa", Description = ""});
            context.UserGroups.Add(new UserGroup { UserId = userId, ChatGroupId = groupId, Type = MemberType.Administrator });
            await context.SaveChangesAsync();

            var updatedGroup = new Group {Id = groupId, Name = "Nowa Nazwa", Description = "Nowy Opis"};

            await service.UpdateGroupAsync(updatedGroup, userId);

            var dbGroup = await context.Groups.FindAsync(groupId);
            Assert.Equal("Nowa Nazwa", dbGroup.Name);
            Assert.Equal("Nowy Opis", dbGroup.Description);
        }
    }
}