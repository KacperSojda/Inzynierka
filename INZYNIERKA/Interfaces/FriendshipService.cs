using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services
{
    public class FriendshipService : IFriendshipService
    {
        private readonly INZDbContext context;

        public FriendshipService(INZDbContext context)
        {
            this.context = context;
        }

        public async Task<bool> AcceptFriendRequestAsync(string currentUserId, int notificationId)
        {
            var notification = await context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.ReceiverId == currentUserId && n.Type == NotificationType.FriendRequest);

            if (notification == null)
            {
                return false;
            }

            var existingRecord = await context.UserFriends.FirstOrDefaultAsync(f =>
                (f.UserId == notification.SenderId && f.FriendId == notification.ReceiverId));

            if (existingRecord != null)
            {
                context.UserFriends.Remove(existingRecord);
            }

            context.UserFriends.AddRange(
                new UserFriend { UserId = notification.SenderId, FriendId = notification.ReceiverId, Status = FriendshipStatus.Accepted },
                new UserFriend { UserId = notification.ReceiverId, FriendId = notification.SenderId, Status = FriendshipStatus.Accepted }
            );

            context.Notifications.Remove(notification);
            await context.SaveChangesAsync();

            return true;
        }
        public async Task<List<FriendViewModel>> GetFriendListAsync(string userId)
        {
            return await context.UserFriends
                .Where(f => f.UserId == userId && f.Status == FriendshipStatus.Accepted)
                .Select(f => new FriendViewModel
                {
                    Id = f.Friend.Id,
                    UserName = f.Friend.UserName
                })
                .ToListAsync();
        }

        public async Task DeleteFriendAsync(string currentUserId, string friendId)
        {
            var friendship1 = await context.UserFriends
                .FirstOrDefaultAsync(f => f.UserId == currentUserId && f.FriendId == friendId);

            var friendship2 = await context.UserFriends
                .FirstOrDefaultAsync(f => f.FriendId == currentUserId && f.UserId == friendId);

            if (friendship1 != null) context.UserFriends.Remove(friendship1);
            if (friendship2 != null) context.UserFriends.Remove(friendship2);

            await context.SaveChangesAsync();
        }

        public async Task<List<FriendViewModel>> GetRequestListAsync(string userId)
        {
            return await context.UserFriends
                .Where(f => f.UserId == userId && f.Status == FriendshipStatus.Pending)
                .Select(f => new FriendViewModel
                {
                    Id = f.Friend.Id,
                    UserName = f.Friend.UserName
                })
                .ToListAsync();
        }

        public async Task DeleteRequestAsync(string currentUserId, string friendId)
        {
            var friendship = await context.UserFriends
                .FirstOrDefaultAsync(f => f.UserId == currentUserId && f.FriendId == friendId);

            if (friendship != null) context.UserFriends.Remove(friendship);

            var notification = await context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.Type == NotificationType.FriendRequest &&
                    n.SenderId == currentUserId &&
                    n.ReceiverId == friendId);

            if (notification != null) context.Notifications.Remove(notification);

            await context.SaveChangesAsync();
        }
    }
}