namespace INZYNIERKA.ViewModels
{
    public class GroupViewModel
    {
        public List<GroupItem> Groups { get; set; } = new List<GroupItem>();
    }

    public class GroupItem
    {
        public int GroupId { get; set; }
        public string Name { get; set; }
    }
}
