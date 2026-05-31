namespace INZYNIERKA.Services.ViewModels
{

    public class SearchByTagsViewModel
    {
        public List<TagCheckboxItem> AvailableTags { get; set; } = new();
        public List<UserViewModel> MatchingUsers { get; set; } = new();
        public int CurrentIndex { get; set; } = 0;
        public string? SearchName { get; set; }
        public string? SearchCity { get; set; }
        public string? SearchCountry { get; set; }
    }

    public class TagCheckboxItem
    {
        public int TagId { get; set; }
        public string TagName { get; set; }
        public bool Selected { get; set; }
    }
}