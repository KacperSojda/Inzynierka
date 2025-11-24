using INZYNIERKA.Models;

namespace INZYNIERKA.ViewModels
{
    public class NotificationListViewModel
    {
        public List<NotificationViewModel> Notifications { get; set; }

    }

    public class NotificationViewModel
    {
        public int Id { get; set; }
        public string SenderUserName { get; set; }
        public string GroupName { get; set; } = "";
        public NotificationType NotificationType { get; set; }
        public DateTime CreationDate { get; set; }
    }
}
