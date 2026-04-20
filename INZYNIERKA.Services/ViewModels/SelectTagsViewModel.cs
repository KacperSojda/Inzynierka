namespace INZYNIERKA.Services.ViewModels
{
    public class SelectTagsViewModel
    {
        public List<TagItem> Tags { get; set; }
    }

    public class TagItem
    {
        public int TagId { get; set; }
        public string TagName { get; set; }
        public bool IsSelected { get; set; }
    }
}
