using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace INZYNIERKA.Tests.Services
{
    public class ChatServiceTests
    {
        private INZDbContext<User> CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<INZDbContext<User>>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new INZDbContext<User>(options);
        }

        private Mock<UserManager<User>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        }

        private Mock<SignInManager<User>> CreateMockSignInManager(UserManager<User> userManager)
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();

            var context = new DefaultHttpContext();
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Name, "TestUser") }));
            context.User = claimsPrincipal;
            contextAccessor.Setup(a => a.HttpContext).Returns(context);

            return new Mock<SignInManager<User>>(userManager, contextAccessor.Object, claimsFactory.Object, null, null, null, null);
        }

        // TESTS FOR: SaveMessage //

        [Fact]
        public async Task SaveMessage_CreatesMessageAndNotification()
        {
            var context = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var service = new ChatService<User>(context, mockUserManager.Object, null, null);

            var senderId = "sender1";
            var receiverId = "receiver1";

            await service.SaveMessage(senderId, receiverId, "Hello", false);

            var savedMessage = await context.Messages.FirstOrDefaultAsync();
            var notification = await context.Notifications.FirstOrDefaultAsync();

            Assert.NotNull(savedMessage);
            Assert.Equal("Hello", savedMessage.Content);
            Assert.NotNull(notification);
            Assert.Equal(receiverId, notification.ReceiverId);
        }

        // TESTS FOR: SaveGroupMessage //

        [Fact]
        public async Task SaveGroupMessage_ThrowsUnauthorized()
        {
            var context = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager.Object);

            var senderId = "UserA";

            mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                           .ReturnsAsync(new User { Id = senderId, UserName = "SenderName" });

            var service = new ChatService<User>(context, mockUserManager.Object, mockSignInManager.Object, null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.SaveGroupMessage(999, senderId, "Message"));
        }

        [Fact]
        public async Task SaveGroupMessage_CreatesMessageAndNotifications()
        {
            var context = CreateInMemoryDbContext();
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager.Object);

            var groupId = 1;
            var senderId = "UserA";
            var memberId = "UserB";

            mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                           .ReturnsAsync(new User { Id = senderId, UserName = "SenderName" });

            var service = new ChatService<User>(context, mockUserManager.Object, mockSignInManager.Object, null);

            var group = new Group { Id = groupId, Name = "Test Group", Description = "Test Description" };
            context.Groups.Add(group);
            context.UserGroups.AddRange(
                new UserGroup { ChatGroupId = groupId, UserId = senderId },
                new UserGroup { ChatGroupId = groupId, UserId = memberId }
            );
            await context.SaveChangesAsync();

            await service.SaveGroupMessage(groupId, senderId, "Group message");

            var msg = await context.GroupMessages.FirstOrDefaultAsync();
            var notif = await context.Notifications.FirstOrDefaultAsync(n => n.GroupId == groupId);

            Assert.NotNull(msg);
            Assert.Equal("Group message", msg.Content);
            Assert.NotNull(notif);
            Assert.Equal(memberId, notif.ReceiverId);
        }

        // TESTS FOR: ClearNotification //

        [Fact]
        public async Task ClearNotification_RemovesNotification()
        {
            var context = CreateInMemoryDbContext();
            var service = new ChatService<User>(context, null, null, null);

            var userId = "user1";
            var friendId = "friend1";

            context.Notifications.Add(new Notification { SenderId = friendId, ReceiverId = userId, Type = NotificationType.Message });
            await context.SaveChangesAsync();

            await service.ClearNotification(userId, friendId);

            var count = await context.Notifications.CountAsync();
            Assert.Equal(0, count);
        }

        // TESTS FOR: SaveImage //

        [Fact]
        public async Task SaveImage_ReturnsFalse()
        {
            var service = new ChatService<User>(null, null, null, null);

            var result = await service.SaveImage("user1", "user2", "", "image/png");

            Assert.False(result);
        }
    }
}