namespace INZYNIERKA.Models
{
    public class UserTag
    {
        public string UserId { get; set; }
        public User User { get; set; }

        public int TagId { get; set; }
        public Tag Tag { get; set; }
    }
}
