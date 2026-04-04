using INZYNIERKA.ViewModels;

namespace INZYNIERKA.Services
{
    public interface IChatService
    {
        Task<ChatViewModel> GetPrivateChatAsync(string currentUserId, string friendId, string userMessage, string geminiAnswer);
        Task<GroupChatViewModel> GetGroupChatAsync(string currentUserId, int groupId, string userMessage, string geminiAnswer);
        Task SavePrivateMessageAsync(string senderId, string receiverId, string content);
        Task ClearMessageNotificationAsync(string userId, string friendId);
    }
}