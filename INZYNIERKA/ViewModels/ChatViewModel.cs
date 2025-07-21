using INZYNIERKA.Models;

namespace INZYNIERKA.ViewModels
{
    public class ChatViewModel
    {
        public string FriendId { get; set; }
        public string CurrentUserId { get; set; }
        public List<Message> Messages { get; set; }
    }
}
