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