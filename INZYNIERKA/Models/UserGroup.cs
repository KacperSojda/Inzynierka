namespace INZYNIERKA.Models
{
    public class UserGroup
    {
        public string UserId { get; set; }
        public User User { get; set; }

        public int ChatGroupId { get; set; }
        public Group ChatGroup { get; set; }
    }
}
