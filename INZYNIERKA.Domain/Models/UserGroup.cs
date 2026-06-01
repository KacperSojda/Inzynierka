namespace INZYNIERKA.Domain.Models
{
    public class UserGroup
    {
        public string UserId { get; set; }
        public User User { get; set; }

        public int ChatGroupId { get; set; }
        public Group ChatGroup { get; set; }

        public MemberType Type { get; set; }

        public string Tone { get; set; } = "casual";
        public string? Custom { get; set; }
        public bool SmartReplies { get; set; } = true;
    }

    public enum MemberType
    {
        Member,
        Administrator,
        Banned
    }
}
