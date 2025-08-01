using INZYNIERKA.Models;

namespace INZYNIERKA.ViewModels
{
    public class ChatViewModel
    {
        public string FriendId { get; set; }
        public string CurrentUserId { get; set; }
        public List<Message> Messages { get; set; }
        public string GeminiQuestion { get; set; }
        public string GeminiAnswer { get; set; }
    }
}
