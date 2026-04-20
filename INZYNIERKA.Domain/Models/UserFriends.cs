namespace INZYNIERKA.Domain.Models
{
    public class UserFriend
    {
        public string UserId { get; set; }
        public User User { get; set; }

        public string FriendId { get; set; }
        public User Friend { get; set; }

        public FriendshipStatus Status { get; set; }
    }
    public enum FriendshipStatus
    {
        Pending,
        Accepted
    }
}
