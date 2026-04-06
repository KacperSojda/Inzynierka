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
            var userId = "odbiorca-id";
            var senderId = "nadawca-id";

            var user = new User { Id = userId, UserName = "Odbiorca", Avatar = "", PublicDescription = "", PrivateDescription = "" };
            var sender = new User { Id = senderId, UserName = "JanKowalski", Avatar = "", PublicDescription = "", PrivateDescription = "" };
            var group = new Group { Id = 1, Name = "GrupaC#", Description = "Opis testowy grupy"};

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
                SenderId = "losowe-id",
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

            Assert.Equal("JanKowalski", result.Notifications[1].SenderUserName);
            Assert.Equal("GrupaC#", result.Notifications[1].GroupName);
        }

        [Fact]
        public async Task GetNotificationsAsyncTest2()
        {
            var context = CreateInMemoryDbContext();
            var service = new NotificationService(context);

            var result = await service.GetNotificationsAsync("brak-usera");

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

            var result = await service.DeleteNotificationAsync("user-id", 999);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteNotificationAsyncTest2()
        {
            var context = CreateInMemoryDbContext();
            var notification = new Notification {Id = 1, ReceiverId = "prawdziwy-wlasciciel", SenderId = "nadawca"};
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            var service = new NotificationService(context);

            var result = await service.DeleteNotificationAsync("haker-id", 1);

            Assert.False(result);
            Assert.Single(await context.Notifications.ToListAsync());
        }

        [Fact]
        public async Task DeleteNotificationAsync_WhenStandardType_DeletesOnlyNotificationAndReturnsTrue()
        {
            var context = CreateInMemoryDbContext();
            var userId = "odbiorca-test3";
            var senderId = "nadawca-test3";

            context.Users.Add(new User { Id = userId, UserName = "Odbiorca3", Avatar = "", PublicDescription = "", PrivateDescription = "" });
            context.Users.Add(new User { Id = senderId, UserName = "Nadawca3", Avatar = "", PublicDescription = "", PrivateDescription = "" });
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
            var receiverId = "odbiorca";
            var senderId = "nadawca";

            context.Users.Add(new User { Id = receiverId, UserName = "Odb", Avatar = "", PublicDescription = "", PrivateDescription = "" });
            context.Users.Add(new User { Id = senderId, UserName = "Nadw", Avatar = "", PublicDescription = "", PrivateDescription = "" });

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