using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Tests.Services
{
    public class FriendshipServiceTests
    {
        private INZDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<INZDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new INZDbContext(options);
        }

        // TESTY DLA: SendRequest //

        [Fact]
        public async Task SendRequestTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService<User>(context);
            var senderId = "Ja";
            var receiverId = "Znajomy";

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
        public async Task SendRequestTest2()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService<User>(context);
            var senderId = "Ja";
            var receiverId = "Znajomy";

            context.UserFriends.Add(new UserFriend { UserId = receiverId, FriendId = senderId, Status = FriendshipStatus.Pending });
            await context.SaveChangesAsync();

            await service.SendRequest(senderId, receiverId);

            var newFriendship = await context.UserFriends
                .FirstOrDefaultAsync(f => f.UserId == senderId && f.FriendId == receiverId);

            Assert.Null(newFriendship);
        }

        // TESTY DLA: AcceptRequest //

        [Fact]
        public async Task AcceptRequestTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService<User>(context);

            var result = await service.AcceptRequest("Ja", 999);

            Assert.False(result);
        }

        [Fact]
        public async Task AcceptRequestTest2()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService<User>(context);

            var senderId = "Ja";
            var receiverId = "Znajomy";
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

        // TESTY DLA: FriendList //

        [Fact]
        public async Task FriendListTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService<User>(context);
            var userId = "Ja";

            context.Users.AddRange(
                new User {Id = "znajomy1", UserName = "Znajomy1", Avatar = "", PublicDescription = "", PrivateDescription = ""},
                new User {Id = "znajomy2", UserName = "Znajomy2", Avatar = "", PublicDescription = "", PrivateDescription = ""}
            );

            context.UserFriends.AddRange(
                new UserFriend {UserId = userId, FriendId = "znajomy1", Status = FriendshipStatus.Accepted},
                new UserFriend {UserId = userId, FriendId = "znajomy2", Status = FriendshipStatus.Pending}
            );
            await context.SaveChangesAsync();

            var (result, totalCount) = await service.FriendList(userId, "", 1, 10);

            Assert.Single(result);
            Assert.Equal("znajomy1", result.First().Id);
            Assert.Equal("Znajomy1", result.First().UserName);
        }

        // TESTY DLA: RequestList //

        [Fact]
        public async Task RequestListTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService<User>(context);
            var userId = "Ja";

            context.Users.AddRange(
                new User {Id = "znajomy1", UserName = "Znajomy1", Avatar = "", PublicDescription = "", PrivateDescription = "" },
                new User {Id = "znajomy2", UserName = "Znajomy2", Avatar = "", PublicDescription = "", PrivateDescription = "" }
            );

            context.UserFriends.AddRange(
                new UserFriend {UserId = userId, FriendId = "znajomy1", Status = FriendshipStatus.Accepted},
                new UserFriend {UserId = userId, FriendId = "znajomy2", Status = FriendshipStatus.Pending}
            );
            await context.SaveChangesAsync();

            var (result, totalCount) = await service.RequestList(userId, 1, 10);

            Assert.Single(result);
            Assert.Equal("znajomy2", result.First().Id);
            Assert.Equal("Znajomy2", result.First().UserName);
        }

        // TESTY DLA: DeleteFriend //

        [Fact]
        public async Task DeleteFriendTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService<User>(context);
            var userId = "Ja";
            var friendId = "Znajomy";

            context.UserFriends.AddRange(
                new UserFriend {UserId = userId, FriendId = friendId, Status = FriendshipStatus.Accepted},
                new UserFriend {UserId = friendId, FriendId = userId, Status = FriendshipStatus.Accepted}
            );
            await context.SaveChangesAsync();

            await service.DeleteFriend(userId, friendId);

            var friendshipsLeft = await context.UserFriends.CountAsync();
            Assert.Equal(0, friendshipsLeft);
        }

        // TESTY DLA: DeleteRequest //

        [Fact]
        public async Task DeleteRequestTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService<User>(context);
            var senderId = "Ja";
            var receiverId = "Znajomy";

            context.UserFriends.Add(new UserFriend {UserId = senderId, FriendId = receiverId, Status = FriendshipStatus.Pending});
            context.Notifications.Add(new Notification {SenderId = senderId, ReceiverId = receiverId, Type = NotificationType.FriendRequest});
            await context.SaveChangesAsync();

            await service.DeleteRequest(senderId, receiverId);

            var friendshipsLeft = await context.UserFriends.CountAsync();
            var notificationsLeft = await context.Notifications.CountAsync();

            Assert.Equal(0, friendshipsLeft);
            Assert.Equal(0, notificationsLeft);
        }
    }
}