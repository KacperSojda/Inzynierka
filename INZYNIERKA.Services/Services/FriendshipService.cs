using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services.Services
{
    public class FriendshipService<TUser> : IFriendshipService<TUser> where TUser : User
    {
        private readonly INZDbContext<TUser> _context;

        public FriendshipService(INZDbContext<TUser> context)
        {
            _context = context;
        }

        /// <summary>Accepts a friend request and creates friendship records.</summary>
        /// <param name="currentUserId">The ID of the user accepting the request.</param>
        /// <param name="notificationId">The ID of the friend request notification.</param>
        /// <returns>True if the request was accepted successfully, otherwise false.</returns>
        public async Task<bool> AcceptRequest(string currentUserId, int notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.ReceiverId == currentUserId && n.Type == NotificationType.FriendRequest);

            if (notification == null) return false;

            var existingRecord = await _context.UserFriends.FirstOrDefaultAsync(f =>
                (f.UserId == notification.SenderId && f.FriendId == notification.ReceiverId));

            if (existingRecord != null)
            {
                _context.UserFriends.Remove(existingRecord);
            }

            _context.UserFriends.AddRange(
                new UserFriend { UserId = notification.SenderId, FriendId = notification.ReceiverId, Status = FriendshipStatus.Accepted },
                new UserFriend { UserId = notification.ReceiverId, FriendId = notification.SenderId, Status = FriendshipStatus.Accepted }
            );

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>Retrieves a paginated and filtered list of a user's friends.</summary>
        /// <param name="userId">The ID of the user whose friends are being retrieved.</param>
        /// <param name="searchQuery">An optional search string to filter friends by name.</param>
        /// <param name="page">The page number for pagination.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A tuple containing the list of friends and the total count of matches.</returns>
        public async Task<(List<FriendViewModel> Friends, int TotalCount)> FriendList(string userId, string? searchQuery, int page, int pageSize)
        {
            var friends = _context.UserFriends
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

        /// <summary>Removes a friend by deleting friendship records between two users.</summary>
        /// <param name="currentUserId">The ID of the current user.</param>
        /// <param name="friendId">The ID of the friend to remove.</param>
        public async Task DeleteFriend(string currentUserId, string friendId)
        {
            var friendship1 = await _context.UserFriends
                .FirstOrDefaultAsync(f => f.UserId == currentUserId && f.FriendId == friendId);

            var friendship2 = await _context.UserFriends
                .FirstOrDefaultAsync(f => f.FriendId == currentUserId && f.UserId == friendId);

            if (friendship1 != null) _context.UserFriends.Remove(friendship1);
            if (friendship2 != null) _context.UserFriends.Remove(friendship2);
            await _context.SaveChangesAsync();
        }

        /// <summary>Retrieves a list of pending friend requests for a user.</summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="page">The page number for pagination.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A tuple containing the list of pending requests and the total count.</returns>
        public async Task<(List<FriendViewModel> Requests, int TotalCount)> RequestList(string userId, int page, int pageSize)
        {
            var query = _context.UserFriends
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

        /// <summary>Cancels or declines a pending friend request and removes the associated notification.</summary>
        /// <param name="currentUserId">The ID of the current user.</param>
        /// <param name="friendId">The ID of the other user involved in the request.</param>
        public async Task DeleteRequest(string currentUserId, string friendId)
        {
            var friendship = await _context.UserFriends
                .FirstOrDefaultAsync(f => f.UserId == currentUserId && f.FriendId == friendId);

            if (friendship != null) _context.UserFriends.Remove(friendship);

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.Type == NotificationType.FriendRequest &&
                    n.SenderId == currentUserId &&
                    n.ReceiverId == friendId);

            if (notification != null) _context.Notifications.Remove(notification);

            await _context.SaveChangesAsync();
        }

        /// <summary>Sends a friend request to another user and creates a notification.</summary>
        /// <param name="senderId">The ID of the user sending the request.</param>
        /// <param name="receiverId">The ID of the user receiving the request.</param>
        public async Task SendRequest(string senderId, string receiverId)
        {
            if (senderId == receiverId) return;

            var existingFriendship = await _context.UserFriends.FirstOrDefaultAsync(f =>
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

            _context.Notifications.Add(notification);

            var friendRequestSender = new UserFriend
            {
                UserId = senderId,
                FriendId = receiverId,
                Status = FriendshipStatus.Pending
            };

            _context.UserFriends.Add(friendRequestSender);

            await _context.SaveChangesAsync();
        }
    }
}