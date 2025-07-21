using INZYNIERKA.ViewModels;

public class SearchByTagsViewModel
{
    public List<TagCheckboxItem> AvailableTags { get; set; } = new();
    public List<UserViewModel> MatchingUsers { get; set; } = new();
    public int CurrentIndex { get; set; } = 0;
}

public class TagCheckboxItem
{
    public int TagId { get; set; }
    public string TagName { get; set; }
    public bool IsSelected { get; set; }
}