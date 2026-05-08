using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INZDbContext context;

        public NotificationService(INZDbContext context)
        {
            this.context = context;
        }

        public async Task<NotificationListViewModel> GetNotificationsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new NotificationListViewModel { Notifications = new List<NotificationViewModel>() };
            }

            var notifications = await context.Notifications
                .Include(n => n.Sender)
                .Include(n => n.Group)
                .Where(n => n.ReceiverId == userId)
                .OrderByDescending(n => n.CreationDate)
                .ToListAsync();

            return new NotificationListViewModel
            {
                Notifications = notifications.Select(n => new NotificationViewModel
                {
                    Id = n.Id,
                    SenderUserName = n.Sender != null ? n.Sender.UserName : "System",
                    GroupName = n.Group != null ? n.Group.Name : "None",
                    NotificationType = n.Type,
                    CreationDate = n.CreationDate
                }).ToList()
            };
        }

        public async Task<bool> DeleteNotificationAsync(string currentUserId, int notificationId)
        {
            if (string.IsNullOrWhiteSpace(currentUserId) || notificationId <= 0) return false;

            var notification = await context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.ReceiverId == currentUserId);

            if (notification == null) return false;

            if (notification.Type == NotificationType.FriendRequest)
            {
                var record = await context.UserFriends.FirstOrDefaultAsync(f =>
                    (f.UserId == notification.SenderId && f.FriendId == notification.ReceiverId));

                if (record != null)
                {
                    context.UserFriends.Remove(record);
                }
            }

            context.Notifications.Remove(notification);
            await context.SaveChangesAsync();

            return true;
        }
    }
}