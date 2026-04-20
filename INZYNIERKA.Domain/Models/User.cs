using Microsoft.AspNetCore.Identity;

namespace INZYNIERKA.Domain.Models
{
    public class User : IdentityUser
    {
        public string PublicDescription { get; set; }
        public string PrivateDescription { get; set; }
        public string Avatar { get; set; }
        public List<UserTag> UserTags { get; set; }
        public List<Notification> SendedNotifications { get; set; }
        public List<Notification> ReceivedNotifications { get; set; }
        public List<UserFriend> SendedFriendRequests { get; set; }
        public List<UserFriend> ReceivedFriendRequests { get; set; }
        public List<Message> SendedMessages { get; set; }
        public List<Message> ReceivedMessages { get; set; }
        public List<UserGroup> JoinedGroups { get; set; }
    }
}
