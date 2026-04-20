using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Tests.Services
{
    public class NotificationServiceTests
    {
        private INZDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<INZDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new INZDbContext(options);
        }

        // TESTY DLA: GetNotificationsAsync //

        [Fact]
        public async Task GetNotificationsAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var userId = "ja";
            var senderId = "znajomy";

            var user = new User {Id = userId, UserName = "Ja", Avatar = "", PublicDescription = "", PrivateDescription = ""};
            var sender = new User {Id = senderId, UserName = "Znajomy", Avatar = "", PublicDescription = "", PrivateDescription = ""};
            var group = new Group {Id = 1, Name = "Grupa Testowa", Description = "Opis testowy grupy"};

            context.Users.AddRange(user, sender);
            context.Groups.Add(group);

            var notification1 = new Notification
            {
                Id = 1,
                ReceiverId = userId,
                SenderId = sender.Id,
                GroupId = group.Id,
                Type = NotificationType.GroupMessage,
                CreationDate = new DateTime(2023, 1, 1)
            };

            var notification2 = new Notification
            {
                Id = 2,
                ReceiverId = userId,
                SenderId = "Random",
                GroupId = null,
                Type = NotificationType.GroupMessage,
                CreationDate = new DateTime(2023, 1, 2)
            };

            context.Notifications.AddRange(notification1, notification2);
            await context.SaveChangesAsync();

            var service = new NotificationService(context);

            var result = await service.GetNotificationsAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Notifications.Count);

            Assert.Equal(2, result.Notifications[0].Id);
            Assert.Equal(1, result.Notifications[1].Id);

            Assert.Equal("System", result.Notifications[0].SenderUserName);
            Assert.Equal("Error", result.Notifications[0].GroupName);

            Assert.Equal("Znajomy", result.Notifications[1].SenderUserName);
            Assert.Equal("Grupa Testowa", result.Notifications[1].GroupName);
        }

        [Fact]
        public async Task GetNotificationsAsyncTest2()
        {
            var context = CreateInMemoryDbContext();
            var service = new NotificationService(context);

            var result = await service.GetNotificationsAsync("brak usera");

            Assert.NotNull(result);
            Assert.NotNull(result.Notifications);
            Assert.Empty(result.Notifications);
        }

        // TESTY DLA DeleteNotificationAsync //

        [Fact]
        public async Task DeleteNotificationAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new NotificationService(context);

            var result = await service.DeleteNotificationAsync("Random", 999);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteNotificationAsyncTest2()
        {
            var context = CreateInMemoryDbContext();
            var notification = new Notification {Id = 1, ReceiverId = "Ja", SenderId = "Znajomy"};
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            var service = new NotificationService(context);

            var result = await service.DeleteNotificationAsync("Nieznajomy", 1);

            Assert.False(result);
            Assert.Single(await context.Notifications.ToListAsync());
        }

        [Fact]
        public async Task DeleteNotificationAsyncTest3()
        {
            var context = CreateInMemoryDbContext();
            var userId = "Ja";
            var senderId = "Znajomy";

            context.Users.Add(new User {Id = userId, UserName = "Ja", Avatar = "", PublicDescription = "", PrivateDescription = ""});
            context.Users.Add(new User {Id = senderId, UserName = "Znajomy", Avatar = "", PublicDescription = "", PrivateDescription = ""});
            await context.SaveChangesAsync();

            var notification = new Notification
            {
                Id = 100,
                ReceiverId = userId,
                SenderId = senderId,
                Type = NotificationType.GroupMessage
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            var service = new NotificationService(context);

            var result = await service.DeleteNotificationAsync(userId, 100);

            Assert.True(result);
            Assert.Empty(await context.Notifications.ToListAsync());
        }

        [Fact]
        public async Task DeleteNotificationAsyncTest4()
        {
            var context = CreateInMemoryDbContext();
            var receiverId = "Ja";
            var senderId = "Znajomy";

            context.Users.Add(new User {Id = receiverId, UserName = "Ja", Avatar = "", PublicDescription = "", PrivateDescription = ""});
            context.Users.Add(new User {Id = senderId, UserName = "Znajomy", Avatar = "", PublicDescription = "", PrivateDescription = ""});

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
                FriendId = receiverId
            };

            context.Notifications.Add(notification);
            context.UserFriends.Add(friendRecord);
            await context.SaveChangesAsync();

            var service = new NotificationService(context);

            var result = await service.DeleteNotificationAsync(receiverId, 1);

            Assert.True(result);
            Assert.Empty(await context.Notifications.ToListAsync());
            Assert.Empty(await context.UserFriends.ToListAsync());
        }
    }
}