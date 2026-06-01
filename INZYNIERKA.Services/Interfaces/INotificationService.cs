using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.ViewModels;

namespace INZYNIERKA.Services.Interfaces
{
    public interface INotificationService<TUser> where TUser : User
    {
        Task<(NotificationListViewModel Model, int TotalCount)> Notifications(string userId, int page = 1, int pageSize = 10);
        Task<bool> DeleteNotification(string currentUserId, int notificationId);
    }
}