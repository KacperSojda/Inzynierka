using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Services;
using INZYNIERKA.Services.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Tests.Services
{
    public class GroupServiceTests
    {
        private INZDbContext<User> CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<INZDbContext<User>>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new INZDbContext<User>(options);
        }

        // TESTS FOR: AvailableGroups //

        [Fact]
        public async Task AvailableGroups_ReturnsGroups()
        {
            var context = CreateInMemoryDbContext();
            var userId = "me";

            var groupJoined = new Group { Id = 1, Name = "Joined Group", Description = "" };
            var groupAvailable = new Group { Id = 2, Name = "Available Group", Description = "" };

            context.Groups.AddRange(groupJoined, groupAvailable);
            context.UserGroups.Add(new UserGroup { UserId = userId, ChatGroupId = 1, Type = MemberType.Member });

            await context.SaveChangesAsync();
            var service = new GroupService<User>(context);

            var (result, totalCount) = await service.AvailableGroups(userId, "", 1, 10);

            Assert.NotNull(result);
            Assert.Single(result.Groups);
            Assert.Equal(2, result.Groups.First().GroupId);
        }

        // TESTS FOR: UserGroups //

        [Fact]
        public async Task UserGroups_ReturnsGroups()
        {
            var context = CreateInMemoryDbContext();
            var userId = "me";

            var myGroup = new Group { Id = 1, Name = "My Group", Description = "" };
            var otherGroup = new Group { Id = 2, Name = "Other Group", Description = "" };

            context.Groups.AddRange(myGroup, otherGroup);
            context.UserGroups.Add(new UserGroup { UserId = userId, ChatGroupId = 1, Type = MemberType.Administrator, ChatGroup = myGroup });

            await context.SaveChangesAsync();
            var service = new GroupService<User>(context);

            var (result, totalCount) = await service.UserGroups(userId, "", 1, 10);

            Assert.NotNull(result);
            Assert.Single(result.Groups);
            Assert.Equal(1, result.Groups.First().GroupId);
        }

        // TESTS FOR: CreateGroup //

        [Fact]
        public async Task CreateGroup_CreatesGroup()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupService<User>(context);
            var userId = "me";

            await service.CreateGroup("My New Group", userId);

            var group = await context.Groups.Include(g => g.Members).FirstOrDefaultAsync();

            Assert.NotNull(group);
            Assert.Equal("My New Group", group.Name);
            Assert.Single(group.Members);
            Assert.Equal(userId, group.Members.First().UserId);
            Assert.Equal(MemberType.Administrator, group.Members.First().Type);
        }

        // TESTS FOR: JoinGroup //

        [Fact]
        public async Task JoinGroup_AddsUserAsMember()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupService<User>(context);
            var groupId = 1;
            var userId = "me";

            context.Groups.Add(new Group { Id = groupId, Name = "Test Group", Description = "" });
            await context.SaveChangesAsync();

            await service.JoinGroup(groupId, userId);

            var membership = await context.UserGroups.FirstOrDefaultAsync(ug => ug.UserId == userId && ug.ChatGroupId == groupId);
            Assert.NotNull(membership);
            Assert.Equal(MemberType.Member, membership.Type);
        }

        // TESTS FOR: LeaveGroup //

        [Fact]
        public async Task LeaveGroup_RemovesMembership()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupService<User>(context);
            var groupId = 1;
            var userId = "me";

            context.UserGroups.Add(new UserGroup { UserId = userId, ChatGroupId = groupId, Type = MemberType.Member });
            await context.SaveChangesAsync();

            await service.LeaveGroup(groupId, userId);

            var membership = await context.UserGroups.FirstOrDefaultAsync(ug => ug.UserId == userId && ug.ChatGroupId == groupId);
            Assert.Null(membership);
        }

        [Fact]
        public async Task LeaveGroup_ThrowsException()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupService<User>(context);
            var groupId = 1;
            var userId = "adminUser";

            context.UserGroups.Add(new UserGroup { UserId = userId, ChatGroupId = groupId, Type = MemberType.Administrator });
            await context.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LeaveGroup(groupId, userId));
            Assert.Contains("You cannot leave the group as the last administrator", exception.Message);
        }

        // TESTS FOR: DeleteGroup //

        [Fact]
        public async Task DeleteGroup_ThrowsUnauthorized()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupService<User>(context);
            var groupId = 1;
            var userId = "memberUser";

            context.Groups.Add(new Group { Id = groupId, Name = "Test Group", Description = "" });
            context.UserGroups.Add(new UserGroup { UserId = userId, ChatGroupId = groupId, Type = MemberType.Member });
            await context.SaveChangesAsync();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteGroup(groupId, userId));
        }

        [Fact]
        public async Task DeleteGroup_RemovesGroup()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupService<User>(context);
            var groupId = 1;
            var userId = "adminUser";

            context.Groups.Add(new Group { Id = groupId, Name = "Test Group", Description = "" });
            context.UserGroups.Add(new UserGroup { UserId = userId, ChatGroupId = groupId, Type = MemberType.Administrator });
            await context.SaveChangesAsync();

            await service.DeleteGroup(groupId, userId);

            var groupExists = await context.Groups.AnyAsync(g => g.Id == groupId);
            Assert.False(groupExists);
        }

        // TESTS FOR: UpdateGroup //

        [Fact]
        public async Task UpdateGroup()
        {
            var context = CreateInMemoryDbContext();
            var service = new GroupService<User>(context);
            var groupId = 1;
            var userId = "adminUser";

            context.Groups.Add(new Group { Id = groupId, Name = "Old Name", Description = "Old Description" });
            context.UserGroups.Add(new UserGroup { UserId = userId, ChatGroupId = groupId, Type = MemberType.Administrator });
            await context.SaveChangesAsync();

            var updatedGroupViewModel = new EditGroupViewModel { Id = groupId, Name = "New Name", Description = "New Description" };

            await service.UpdateGroup(updatedGroupViewModel, userId);

            var dbGroup = await context.Groups.FindAsync(groupId);
            Assert.Equal("New Name", dbGroup.Name);
            Assert.Equal("New Description", dbGroup.Description);
        }
    }
}