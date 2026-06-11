using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services.Services
{
    public class NotificationService<TUser> : INotificationService<TUser> where TUser : User
    {
        private readonly INZDbContext<TUser> _context;

        public NotificationService(INZDbContext<TUser> context)
        {
            _context = context;
        }

        /// <summary>Retrieves a paginated list of notifications for a specific user.</summary>
        /// <param name="userId">The ID of the user receiving the notifications.</param>
        /// <param name="page">The page number for pagination.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A tuple containing the notification list and the total count of notifications.</returns>
        public async Task<(NotificationListViewModel Model, int TotalCount)> Notifications(string userId, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return (new NotificationListViewModel { Notifications = new List<NotificationViewModel>() }, 0);
            }

            var query = _context.Notifications
                .Include(n => n.Sender)
                .Include(n => n.Group)
                .Where(n => n.ReceiverId == userId);

            int totalCount = await query.CountAsync();

            var notifications = await query
                .OrderByDescending(n => n.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new NotificationListViewModel
            {
                Notifications = notifications.Select(n => new NotificationViewModel
                {
                    Id = n.Id,
                    SenderId = n.SenderId ?? "System",
                    SenderName = n.Sender != null ? n.Sender.UserName : "System",
                    GroupId = n.GroupId ?? 0,
                    GroupName = n.Group != null ? n.Group.Name : "None",
                    NotificationType = n.Type,
                    Timestamp = n.Timestamp
                }).ToList()
            };

            return (model, totalCount);
        }

        /// <summary>Deletes a specific notification and removes pending friend requests.</summary>
        /// <param name="currentUserId">The ID of the user owning the notification.</param>
        /// <param name="notificationId">The ID of the notification to delete.</param>
        /// <returns>True if the notification was successfully deleted, otherwise false.</returns>
        public async Task<bool> DeleteNotification(string currentUserId, int notificationId)
        {
            if (string.IsNullOrWhiteSpace(currentUserId) || notificationId <= 0) return false;

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.ReceiverId == currentUserId);

            if (notification == null) return false;

            if (notification.Type == NotificationType.FriendRequest)
            {
                var record = await _context.UserFriends.FirstOrDefaultAsync(f =>
                    (f.UserId == notification.SenderId && f.FriendId == notification.ReceiverId));

                if (record != null)
                {
                    _context.UserFriends.Remove(record);
                }
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}