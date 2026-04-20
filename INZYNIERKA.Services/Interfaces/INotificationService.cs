using INZYNIERKA.Services.ViewModels;

namespace INZYNIERKA.Services.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationListViewModel> GetNotificationsAsync(string userId);
        Task<bool> DeleteNotificationAsync(string currentUserId, int notificationId);
    }
}