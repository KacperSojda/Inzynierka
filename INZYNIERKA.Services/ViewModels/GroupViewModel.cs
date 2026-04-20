namespace INZYNIERKA.Services.ViewModels
{
    public class GroupViewModel
    {
        public List<GroupItem> AdminGroups { get; set; } = new();
        public List<GroupItem> Groups { get; set; } = new();
    }

    public class GroupItem
    {
        public int GroupId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Tags { get; set; } = new List<String>();
    }
}
