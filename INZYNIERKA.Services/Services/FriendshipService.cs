using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services.Services
{
    public class FriendshipService<TUser> : IFriendshipService<TUser> where TUser : User
    {
        private readonly INZDbContext<TUser> context;

        public FriendshipService(INZDbContext<TUser> context)
        {
            this.context = context;
        }

        public async Task<bool> AcceptRequest(string currentUserId, int notificationId)
        {
            var notification = await context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.ReceiverId == currentUserId && n.Type == NotificationType.FriendRequest);

            if (notification == null) return false;

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
        public async Task<(List<FriendViewModel> Friends, int TotalCount)> FriendList(string userId, string? searchQuery, int page, int pageSize)
        {
            var friends = context.UserFriends
                .Include(f => f.Friend)
                .Where(f => f.UserId == userId);

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                searchQuery = searchQuery.Trim().ToLower();

                friends = friends.Where(f => f.Friend.UserName.Trim().ToLower().Contains(searchQuery));
            }

            int totalCount = await friends.CountAsync();

            var result = await friends
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = result.Select(f => new FriendViewModel
            {
                Id = f.Friend.Id,
                UserName = f.Friend.UserName,
                IsDeleted = f.Friend.LockoutEnd.HasValue
            }).ToList();

            return (model, totalCount);
        }

        public async Task DeleteFriend(string currentUserId, string friendId)
        {
            var friendship1 = await context.UserFriends
                .FirstOrDefaultAsync(f => f.UserId == currentUserId && f.FriendId == friendId);

            var friendship2 = await context.UserFriends
                .FirstOrDefaultAsync(f => f.FriendId == currentUserId && f.UserId == friendId);

            if (friendship1 != null) context.UserFriends.Remove(friendship1);
            if (friendship2 != null) context.UserFriends.Remove(friendship2);

            await context.SaveChangesAsync();
        }

        public async Task<(List<FriendViewModel> Requests, int TotalCount)> RequestList(string userId, int page, int pageSize)
        {
            var query = context.UserFriends
                .Where(f => f.UserId == userId && f.Status == FriendshipStatus.Pending);

            int totalCount = await query.CountAsync();

            var requests = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FriendViewModel
                {
                    Id = f.Friend.Id,
                    UserName = f.Friend.UserName,
                    IsDeleted = f.Friend.LockoutEnd.HasValue
                })
                .ToListAsync();

            return (requests, totalCount);
        }

        public async Task DeleteRequest(string currentUserId, string friendId)
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

        public async Task SendRequest(string senderId, string receiverId)
        {
            if (senderId == receiverId) return;

            var existingFriendship = await context.UserFriends.FirstOrDefaultAsync(f =>
                (f.UserId == receiverId && f.FriendId == senderId) ||
                (f.UserId == senderId && f.FriendId == receiverId));

            if (existingFriendship != null) return;

            var notification = new Notification
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Type = NotificationType.FriendRequest,
                Timestamp = DateTime.UtcNow
            };

            context.Notifications.Add(notification);

            var friendRequestSender = new UserFriend
            {
                UserId = senderId,
                FriendId = receiverId,
                Status = FriendshipStatus.Pending
            };

            context.UserFriends.Add(friendRequestSender);

            await context.SaveChangesAsync();
        }
    }
}