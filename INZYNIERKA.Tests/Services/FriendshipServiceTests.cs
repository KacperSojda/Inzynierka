using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.Services;
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

        // TESTY DLA: SendFriendRequestAsync //

        [Fact]
        public async Task SendFriendRequestAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService(context);
            var senderId = "Ja";
            var receiverId = "Znajomy";

            await service.SendFriendRequestAsync(senderId, receiverId);

            var pendingFriendship = await context.UserFriends
                .FirstOrDefaultAsync(f => f.UserId == senderId && f.FriendId == receiverId);

            var notification = await context.Notifications
                .FirstOrDefaultAsync(n => n.SenderId == senderId && n.ReceiverId == receiverId && n.Type == NotificationType.FriendRequest);

            Assert.NotNull(pendingFriendship);
            Assert.Equal(FriendshipStatus.Pending, pendingFriendship.Status);
            Assert.NotNull(notification);
        }

        [Fact]
        public async Task SendFriendRequestAsyncTest2()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService(context);
            var senderId = "Ja";
            var receiverId = "Znajomy";

            context.UserFriends.Add(new UserFriend { UserId = receiverId, FriendId = senderId, Status = FriendshipStatus.Pending });
            await context.SaveChangesAsync();

            await service.SendFriendRequestAsync(senderId, receiverId);

            var newFriendship = await context.UserFriends
                .FirstOrDefaultAsync(f => f.UserId == senderId && f.FriendId == receiverId);

            Assert.Null(newFriendship);
        }

        // TESTY DLA: AcceptFriendRequestAsync //

        [Fact]
        public async Task AcceptFriendRequestAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService(context);

            var result = await service.AcceptFriendRequestAsync("Ja", 999);

            Assert.False(result);
        }

        [Fact]
        public async Task AcceptFriendRequestAsyncTest2()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService(context);

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

            var result = await service.AcceptFriendRequestAsync(receiverId, notificationId);

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

        // TESTY DLA: GetFriendListAsync //

        [Fact]
        public async Task GetFriendListAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService(context);
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

            var result = await service.GetFriendListAsync(userId);

            Assert.Single(result);
            Assert.Equal("znajomy1", result.First().Id);
            Assert.Equal("Znajomy1", result.First().UserName);
        }

        // TESTY DLA: GetRequestListAsync //

        [Fact]
        public async Task GetRequestListAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService(context);
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

            var result = await service.GetRequestListAsync(userId);

            Assert.Single(result);
            Assert.Equal("znajomy2", result.First().Id);
            Assert.Equal("Znajomy2", result.First().UserName);
        }

        // TESTY DLA: DeleteFriendAsync //

        [Fact]
        public async Task DeleteFriendAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService(context);
            var userId = "Ja";
            var friendId = "Znajomy";

            context.UserFriends.AddRange(
                new UserFriend {UserId = userId, FriendId = friendId, Status = FriendshipStatus.Accepted},
                new UserFriend {UserId = friendId, FriendId = userId, Status = FriendshipStatus.Accepted}
            );
            await context.SaveChangesAsync();

            await service.DeleteFriendAsync(userId, friendId);

            var friendshipsLeft = await context.UserFriends.CountAsync();
            Assert.Equal(0, friendshipsLeft);
        }

        // TESTY DLA: DeleteRequestAsync //

        [Fact]
        public async Task DeleteRequestAsyncTest()
        {
            var context = CreateInMemoryDbContext();
            var service = new FriendshipService(context);
            var senderId = "Ja";
            var receiverId = "Znajomy";

            context.UserFriends.Add(new UserFriend {UserId = senderId, FriendId = receiverId, Status = FriendshipStatus.Pending});
            context.Notifications.Add(new Notification {SenderId = senderId, ReceiverId = receiverId, Type = NotificationType.FriendRequest});
            await context.SaveChangesAsync();

            await service.DeleteRequestAsync(senderId, receiverId);

            var friendshipsLeft = await context.UserFriends.CountAsync();
            var notificationsLeft = await context.Notifications.CountAsync();

            Assert.Equal(0, friendshipsLeft);
            Assert.Equal(0, notificationsLeft);
        }
    }
}