using INZYNIERKA.Domain.Models;

namespace INZYNIERKA.Services.ViewModels
{
    public class NotificationListViewModel
    {
        public List<NotificationViewModel> Notifications { get; set; }

    }

    public class NotificationViewModel
    {
        public int Id { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public int GroupId { get; set; } = 0;
        public string GroupName { get; set; } = "";
        public NotificationType NotificationType { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
