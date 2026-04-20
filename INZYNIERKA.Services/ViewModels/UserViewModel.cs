using INZYNIERKA.Domain.Models;

namespace INZYNIERKA.Services.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; } = "";
        public string? Avatar { get; set; }
        public string? UserName { get; set; }
        public string? PublicDescription { get; set; }
        public string? PrivateDescription { get; set; }
        public List<string> Tags { get; set; } = new List<String>();
        public List<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
