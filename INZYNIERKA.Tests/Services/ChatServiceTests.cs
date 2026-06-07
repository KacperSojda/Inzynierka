using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace INZYNIERKA.Tests.Services
{
    public class ChatServiceTests
    {
        private INZDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<INZDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new INZDbContext(options);
        }

        private Mock<UserManager<User>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        }

        // TESTY DLA: Chat //
        /*
        [Fact]
        public async Task ChatTest()
        {
            var context = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();

            mockUserManager.Setup(um => um.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((User)null);

            var service = new ChatService<User>(context, mockUserManager.Object, );

            var result = await service.Chat("user1", "user2", "", "");

            Assert.Null(result);
        }

        [Fact]
        public async Task ChatTest2()
        {
            var context = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();

            var user = new User {Id = "Ja", UserName = "Ja", Avatar = "", PublicDescription = "", PrivateDescription = ""};
            var friend = new User {Id = "Znajomy", UserName = "Znajomy", Avatar = "", PublicDescription = "", PrivateDescription = ""};

            mockUserManager.Setup(um => um.FindByIdAsync("Ja")).ReturnsAsync(user);
            mockUserManager.Setup(um => um.FindByIdAsync("Znajomy")).ReturnsAsync(friend);

            context.Messages.AddRange(
                new Message {Id = 1, SenderId = "Ja", ReceiverId = "Znajomy", Content = "Pierwsza", Timestamp = DateTime.UtcNow, Sender = user, Receiver = friend},
                new Message {Id = 2, SenderId = "Znajomy", ReceiverId = "Ja", Content = "Druga", Timestamp = DateTime.UtcNow.AddMinutes(5), Sender = friend, Receiver = user}
            );
            await context.SaveChangesAsync();

            var service = new ChatService<User>(context, mockUserManager.Object);

            var result = await service.Chat("Ja", "Znajomy", "", "");

            Assert.NotNull(result);
            Assert.Equal("Znajomy", result.FriendId);
            Assert.Equal(2, result.Messages.Count);
            Assert.Equal("Pierwsza", result.Messages.First().Content);
            Assert.Equal("Druga", result.Messages.Last().Content);
        }

        // TESTY DLA: GroupChat //

        [Fact]
        public async Task GroupChatTest()
        {
            var context = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ChatService<User>(context, mockUserManager.Object);

            var groupId = 1;
            var userId = "Ja";

            context.Groups.Add(new Group {Id = groupId, Name = "Grupa Testowa", Description = ""});
            await context.SaveChangesAsync();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GroupChat(userId, groupId, "", ""));
        }

        // TESTY DLA: SaveMessage //

        [Fact]
        public async Task SaveMessageTest()
        {
            var context = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ChatService<User>(context, mockUserManager.Object);

            var senderId = "Ja";
            var receiverId = "Znajomy";

            await service.SaveMessage(senderId, receiverId, "Hej");

            var savedMessage = await context.Messages.FirstOrDefaultAsync();
            var notification = await context.Notifications.FirstOrDefaultAsync();

            Assert.NotNull(savedMessage);
            Assert.Equal("Hej", savedMessage.Content);

            Assert.NotNull(notification);
            Assert.Equal(NotificationType.Message, notification.Type);
        }

        [Fact]
        public async Task SaveMessageTest2()
        {
            var context = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ChatService<User>(context, mockUserManager.Object);

            var senderId = "Ja";
            var receiverId = "Znajomy";

            context.Notifications.Add(new Notification { SenderId = senderId, ReceiverId = receiverId, Type = NotificationType.Message });
            await context.SaveChangesAsync();

            await service.SaveMessage(senderId, receiverId, "Wiadomosc");

            var totalNotifications = await context.Notifications.CountAsync();

            Assert.Equal(1, totalNotifications);
        }

        // TESTY DLA: ClearNotification //

        [Fact]
        public async Task ClearNotificationTest()
        {
            var context = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ChatService<User>(context, mockUserManager.Object);

            var userId = "Ja";
            var friendId = "Znajomy";

            context.Notifications.Add(new Notification {SenderId = friendId, ReceiverId = userId});
            await context.SaveChangesAsync();

            await service.ClearNotification(userId, friendId);

            var notificationsLeft = await context.Notifications.CountAsync();
            Assert.Equal(0, notificationsLeft);
        }

        // TESTY DLA: SaveGroupMessage //

        [Fact]
        public async Task SaveGroupMessageTest()
        {
            var context = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ChatService<User>(context, mockUserManager.Object);

            var groupId = 1;
            var senderId = "Ja";
            var memberId1 = "Znajomy1";
            var memberId2 = "Znajomy2";

            var group = new Group {Id = groupId, Name = "Grupa Testowa", Description = ""};
            context.Groups.Add(group);

            context.UserGroups.AddRange(
                new UserGroup {ChatGroupId = groupId, UserId = senderId, ChatGroup = group},
                new UserGroup {ChatGroupId = groupId, UserId = memberId1, ChatGroup = group},
                new UserGroup {ChatGroupId = groupId, UserId = memberId2, ChatGroup = group}
            );
            await context.SaveChangesAsync();

            await service.SaveGroupMessage(groupId, senderId, "Wiadomosc");

            var savedMessage = await context.GroupMessages.FirstOrDefaultAsync();
            var notifications = await context.Notifications.ToListAsync();

            Assert.NotNull(savedMessage);
            Assert.Equal("Wiadomosc", savedMessage.Content);

            Assert.Equal(2, notifications.Count);
            Assert.DoesNotContain(notifications, n => n.ReceiverId == senderId);
        }*/
    }
}