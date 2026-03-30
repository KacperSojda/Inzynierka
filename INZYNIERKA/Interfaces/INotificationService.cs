using INZYNIERKA.ViewModels;

namespace INZYNIERKA.Services
{
    public interface INotificationService
    {
        Task<NotificationListViewModel> GetNotificationsAsync(string userId);
        Task<bool> DeleteNotificationAsync(string currentUserId, int notificationId);
    }
}