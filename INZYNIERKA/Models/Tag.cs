namespace INZYNIERKA.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<UserTag> UserTags { get; set; }
        public List<GroupTag> GroupTags { get; set; }
    }
}
