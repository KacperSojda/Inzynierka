using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Tests.Services
{
    public class FriendshipServiceTests
    {
        private INZDbContext<User> CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<INZDbContext<User>>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new INZDbContext<User>(options);
        }

        // TESTS FOR: SendRequest //

        [Fact]
        public async Task SendRequest_CreatesRequestAndNotification()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService<User>(context);
            var senderId = "me";
            var receiverId = "friend";

            await service.SendRequest(senderId, receiverId);

            var pendingFriendship = await context.UserFriends
                .FirstOrDefaultAsync(f => f.UserId == senderId && f.FriendId == receiverId);

            var notification = await context.Notifications
                .FirstOrDefaultAsync(n => n.SenderId == senderId && n.ReceiverId == receiverId && n.Type == NotificationType.FriendRequest);

            Assert.NotNull(pendingFriendship);
            Assert.Equal(FriendshipStatus.Pending, pendingFriendship.Status);
            Assert.NotNull(notification);
        }

        [Fact]
        public async Task SendNothing_FriendshiExists()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService<User>(context);
            var senderId = "me";
            var receiverId = "friend";

            context.UserFriends.Add(new UserFriend { UserId = receiverId, FriendId = senderId, Status = FriendshipStatus.Pending });
            await context.SaveChangesAsync();

            await service.SendRequest(senderId, receiverId);

            var newFriendship = await context.UserFriends
                .FirstOrDefaultAsync(f => f.UserId == senderId && f.FriendId == receiverId);

            Assert.Null(newFriendship);
        }

        // TESTS FOR: AcceptRequest //

        [Fact]
        public async Task AcceptRequest_ReturnsFalse_NotificationNotExist()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService<User>(context);

            var result = await service.AcceptRequest("me", 999);

            Assert.False(result);
        }

        [Fact]
        public async Task AcceptRequest_CreatesFriendshipRemovesNotification()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService<User>(context);

            var senderId = "me";
            var receiverId = "friend";
            var notificationId = 1;

            context.Notifications.Add(new Notification
            {
                Id = notificationId,
                SenderId = senderId,
                ReceiverId = receiverId,
                Type = NotificationType.FriendRequest
            });
            await context.SaveChangesAsync();

            var result = await service.AcceptRequest(receiverId, notificationId);

            var friendship1 = await context.UserFriends.FirstOrDefaultAsync(f => f.UserId == senderId && f.FriendId == receiverId);
            var friendship2 = await context.UserFriends.FirstOrDefaultAsync(f => f.UserId == receiverId && f.FriendId == senderId);
            var notificationStillExists = await context.Notifications.AnyAsync(n => n.Id == notificationId);

            Assert.True(result);

            Assert.NotNull(friendship1);
            Assert.Equal(FriendshipStatus.Accepted, friendship1.Status);

            Assert.NotNull(friendship2);
            Assert.Equal(FriendshipStatus.Accepted, friendship2.Status);

            Assert.False(notificationStillExists);
        }

        // TESTS FOR: FriendList //

        [Fact]
        public async Task FriendList_ReturnsFriends()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService<User>(context);
            var userId = "me";

            context.Users.AddRange(
                new User { Id = "friend1", UserName = "Friend1", PrivateDescription = "PrivateDescription1", PublicDescription = "PublicDescription1", Avatar = "DefaultAvatar1" },
                new User { Id = "friend2", UserName = "Friend2", PrivateDescription = "PrivateDescription2", PublicDescription = "PublicDescription2", Avatar = "DefaultAvatar2" }
            );

            context.UserFriends.AddRange(
                new UserFriend { UserId = userId, FriendId = "friend1", Status = FriendshipStatus.Accepted },
                new UserFriend { UserId = userId, FriendId = "friend2", Status = FriendshipStatus.Pending }
            );
            await context.SaveChangesAsync();

            var (result, totalCount) = await service.FriendList(userId, "friend1", 1, 10);

            Assert.Single(result);
            Assert.Equal("friend1", result.First().Id);
            Assert.Equal("Friend1", result.First().UserName);
        }

        // TESTS FOR: RequestList //

        [Fact]
        public async Task RequestList_ReturnsRequests()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService<User>(context);
            var userId = "me";

            context.Users.AddRange(
                new User { Id = "friend1", UserName = "Friend1", PrivateDescription = "PrivateDescription1", PublicDescription = "PublicDescription1", Avatar = "DefaultAvatar1" },
                new User { Id = "friend2", UserName = "Friend2", PrivateDescription = "PrivateDescription2", PublicDescription = "PublicDescription2", Avatar = "DefaultAvatar2" }
            );

            context.UserFriends.AddRange(
                new UserFriend { UserId = userId, FriendId = "friend1", Status = FriendshipStatus.Accepted },
                new UserFriend { UserId = userId, FriendId = "friend2", Status = FriendshipStatus.Pending }
            );
            await context.SaveChangesAsync();

            var (result, totalCount) = await service.RequestList(userId, 1, 10);

            Assert.Single(result);
            Assert.Equal("friend2", result.First().Id);
            Assert.Equal("Friend2", result.First().UserName);
        }

        // TESTS FOR: DeleteFriend //

        [Fact]
        public async Task DeleteFriend_RemovesFriendships()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService<User>(context);
            var userId = "me";
            var friendId = "friend";

            context.UserFriends.AddRange(
                new UserFriend { UserId = userId, FriendId = friendId, Status = FriendshipStatus.Accepted },
                new UserFriend { UserId = friendId, FriendId = userId, Status = FriendshipStatus.Accepted }
            );
            await context.SaveChangesAsync();

            await service.DeleteFriend(userId, friendId);

            var friendshipsLeft = await context.UserFriends.CountAsync();
            Assert.Equal(0, friendshipsLeft);
        }

        // TESTS FOR: DeleteRequest //

        [Fact]
        public async Task DeleteRequest_RemovesRequestAndNotification()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService<User>(context);
            var senderId = "me";
            var receiverId = "friend";

            context.UserFriends.Add(new UserFriend { UserId = senderId, FriendId = receiverId, Status = FriendshipStatus.Pending });
            context.Notifications.Add(new Notification { SenderId = senderId, ReceiverId = receiverId, Type = NotificationType.FriendRequest });
            await context.SaveChangesAsync();

            await service.DeleteRequest(senderId, receiverId);

            var friendshipsLeft = await context.UserFriends.CountAsync();
            var notificationsLeft = await context.Notifications.CountAsync();

            Assert.Equal(0, friendshipsLeft);
            Assert.Equal(0, notificationsLeft);
        }
    }
}