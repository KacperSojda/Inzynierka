namespace INZYNIERKA.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string ReceiverId { get; set; }
        public User Receiver { get; set; } 
        public string SenderId { get; set; }
        public User Sender { get; set; }
        public int? GroupId { get; set; } = null;
        public Group Group { get; set; }
        public NotificationType Type { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
    }
    public enum NotificationType
    {
        FriendRequest,
        Message,
        GroupMessage
    }
}