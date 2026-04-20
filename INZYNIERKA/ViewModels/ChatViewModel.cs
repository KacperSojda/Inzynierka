using INZYNIERKA.Domain.Models;

namespace INZYNIERKA.ViewModels
{
    public class ChatViewModel
    {
        public string FriendId { get; set; }
        public string FriendName { get; set; }
        public string CurrentUserId { get; set; }
        public string CurrentUserName { get; set; }
        public List<MessageViewModel> Messages { get; set; }
        public string UserMessage { get; set;  }
        public string GeminiQuestion { get; set; }
        public string GeminiAnswer { get; set; }
    }

    public class MessageViewModel
    {
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public string Content { get; set; }
        public DateTime DateTime { get; set; }
    }
}
