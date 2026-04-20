namespace INZYNIERKA.Domain.Models
{
    public class GroupTag
    {
        public int GroupId { get; set; }
        public Group Group { get; set; }

        public int TagId { get; set; }
        public Tag Tag { get; set; }
    }
}
