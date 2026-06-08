using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Tests.Services
{
    public class NotificationServiceTests
    {
        private INZDbContext<User> CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<INZDbContext<User>>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new INZDbContext<User>(options);
        }

        // TESTS FOR: Notifications //

        [Fact]
        public async Task Notifications_ReturnsNotifications()
        {
            var context = CreateInMemoryDbContext();
            var userId = "me";
            var senderId = "friend";

            var user = new User { Id = userId, UserName = "Me", Avatar = "DefaultAvatar", PublicDescription = "PublicDescription", PrivateDescription = "PrivateDescription" };
            var sender = new User { Id = senderId, UserName = "Friend", Avatar = "DefaultAvatar", PublicDescription = "PublicDescription", PrivateDescription = "PrivateDescription" };
            var group = new Group { Id = 1, Name = "Test Group", Description = "Test description" };

            context.Users.AddRange(user, sender);
            context.Groups.Add(group);

            var notification1 = new Notification
            {
                Id = 1,
                ReceiverId = userId,
                SenderId = sender.Id,
                GroupId = group.Id,
                Type = NotificationType.GroupMessage,
                Timestamp = new DateTime(2023, 1, 1)
            };

            context.Notifications.AddRange(notification1);
            await context.SaveChangesAsync();

            var service = new NotificationService<User>(context);

            var (result, totalCount) = await service.Notifications(userId, 1, 10);

            Assert.NotNull(result);
            Assert.Equal(1, result.Notifications.Count);

            Assert.Equal(1, result.Notifications[0].Id);


            Assert.Equal("Friend", result.Notifications[0].SenderName);
            Assert.Equal("Test Group", result.Notifications[0].GroupName);
        }

        [Fact]
        public async Task Notifications_ReturnsEmptyList()
        {
            var context = CreateInMemoryDbContext();
            var service = new NotificationService<User>(context);

            var (result, totalCount) = await service.Notifications(" ", 1, 10);

            Assert.NotNull(result);
            Assert.Empty(result.Notifications);
            Assert.Equal(0, totalCount);
        }

        [Fact]
        public async Task Notifications_ReturnsEmptyList_NoNotifications()
        {
            var context = CreateInMemoryDbContext();
            var service = new NotificationService<User>(context);

            var (result, totalCount) = await service.Notifications("nonexistent_user", 1, 10);

            Assert.NotNull(result);
            Assert.Empty(result.Notifications);
            Assert.Equal(0, totalCount);
        }

        // TESTS FOR: DeleteNotification //

        [Fact]
        public async Task DeleteNotification_ReturnsFalse_NotificationNotExist()
        {
            var context = CreateInMemoryDbContext();
            var service = new NotificationService<User>(context);

            var result = await service.DeleteNotification("me", 999);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteNotification_ReturnsFalse_UserIsNotReceiver()
        {
            var context = CreateInMemoryDbContext();
            var notification = new Notification { Id = 1, ReceiverId = "me", SenderId = "friend" };
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            var service = new NotificationService<User>(context);

            var result = await service.DeleteNotification("stranger", 1);

            Assert.False(result);
            Assert.Single(await context.Notifications.ToListAsync());
        }

        [Fact]
        public async Task DeleteNotification_RemovesNotification()
        {
            var context = CreateInMemoryDbContext();
            var userId = "me";
            var senderId = "friend";

            context.Users.Add(new User { Id = userId, UserName = "Me", PrivateDescription = "PrivateDescription", PublicDescription = "PublicDescription", Avatar = "DefaultAvatar" });
            context.Users.Add(new User { Id = senderId, UserName = "Friend", PrivateDescription = "PrivateDescription", PublicDescription = "PublicDescription", Avatar = "DefaultAvatar" });

            var notification = new Notification
            {
                Id = 100,
                ReceiverId = userId,
                SenderId = senderId,
                Type = NotificationType.GroupMessage
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            var service = new NotificationService<User>(context);

            var result = await service.DeleteNotification(userId, 100);

            Assert.True(result);
            Assert.Empty(await context.Notifications.ToListAsync());
        }

        [Fact]
        public async Task DeleteNotification_RemovesNotificationAndFriendRequest()
        {
            var context = CreateInMemoryDbContext();
            var receiverId = "me";
            var senderId = "friend";

            context.Users.Add(new User { Id = receiverId, UserName = "Me", PrivateDescription = "PrivateDescription", PublicDescription = "PublicDescription", Avatar = "DefaultAvatar" });
            context.Users.Add(new User { Id = senderId, UserName = "Friend", PrivateDescription = "PrivateDescription", PublicDescription = "PublicDescription", Avatar = "DefaultAvatar" });

            var notification = new Notification
            {
                Id = 1,
                ReceiverId = receiverId,
                SenderId = senderId,
                Type = NotificationType.FriendRequest
            };

            var friendRecord = new UserFriend
            {
                UserId = senderId,
                FriendId = receiverId,
                Status = FriendshipStatus.Pending
            };

            context.Notifications.Add(notification);
            context.UserFriends.Add(friendRecord);
            await context.SaveChangesAsync();

            var service = new NotificationService<User>(context);

            var result = await service.DeleteNotification(receiverId, 1);

            Assert.True(result);
            Assert.Empty(await context.Notifications.ToListAsync());
            Assert.Empty(await context.UserFriends.ToListAsync());
        }
    }
}