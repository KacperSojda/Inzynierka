namespace INZYNIERKA.Services.ViewModels
{
    public class FriendListViewModel
    {
        public List<FriendViewModel> Friends = new List<FriendViewModel>();
    }
    public class FriendViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
    }
}
