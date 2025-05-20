namespace INZYNIERKA.ViewModels
{
    public class UserViewModel
    {
        public string Avatar { get; set; }
        public string UserName { get; set; }
        public string PublicDescription { get; set; }
        public string PrivateDescription { get; set; }
        public List<string> Tags { get; set; } = new();
    }
}
