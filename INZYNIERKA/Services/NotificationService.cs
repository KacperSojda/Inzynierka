using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services
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
            var user = await context.Users
                .Include(u => u.ReceivedNotifications)
                    .ThenInclude(n => n.Sender)
                .Include(u => u.ReceivedNotifications)
                    .ThenInclude(n => n.Group)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new NotificationListViewModel { Notifications = new List<NotificationViewModel>() };
            }

            return new NotificationListViewModel
            {
                Notifications = user.ReceivedNotifications.Select(n => new NotificationViewModel
                {
                    Id = n.Id,
                    SenderUserName = n.Sender != null ? n.Sender.UserName : "System",
                    GroupName = n.Group != null ? n.Group.Name : "Error",
                    NotificationType = n.Type,
                    CreationDate = n.CreationDate
                }).OrderByDescending(n => n.CreationDate).ToList()
            };
        }

        public async Task<bool> DeleteNotificationAsync(string currentUserId, int notificationId)
        {
            var notification = await context.Notifications
                .Include(n => n.Sender)
                .Include(n => n.Receiver)
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.ReceiverId == currentUserId);

            if (notification == null)
            {
                return false;
            }

            // Jeśli to było zaproszenie do znajomych, usuwamy relację Pending
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