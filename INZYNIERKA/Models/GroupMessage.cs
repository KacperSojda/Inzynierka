namespace INZYNIERKA.Models
{
    public class GroupMessage
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public Group ChatGroup { get; set; }

        public string SenderId { get; set; }
        public User Sender { get; set; }

        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
