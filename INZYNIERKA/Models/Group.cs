namespace INZYNIERKA.Models
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public List<UserGroup> Members { get; set; }
        public List<GroupMessage> Messages { get; set; }
    }
}
