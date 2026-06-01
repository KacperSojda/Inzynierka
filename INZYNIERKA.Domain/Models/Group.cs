namespace INZYNIERKA.Domain.Models
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<UserGroup> Members { get; set; } = new List<UserGroup>();
        public List<GroupMessage> Messages { get; set; } = new List<GroupMessage>();
        public List<GroupTag> GroupTags { get; set; } = new List<GroupTag>();
        public List<Notification> SendedNotifications { get; set; }
    }
}
