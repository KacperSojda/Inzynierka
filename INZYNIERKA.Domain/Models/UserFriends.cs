namespace INZYNIERKA.Domain.Models
{
    public class UserFriend
    {
        public string UserId { get; set; }
        public User User { get; set; }

        public string FriendId { get; set; }
        public User Friend { get; set; }

        public FriendshipStatus Status { get; set; }

        public string Tone { get; set; } = "casual";
        public string? Custom { get; set; }
        public bool SmartReplies { get; set; } = true;
    }
    public enum FriendshipStatus
    {
        Pending,
        Accepted
    }
}
