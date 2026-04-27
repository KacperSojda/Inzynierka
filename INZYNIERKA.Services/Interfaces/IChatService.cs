using INZYNIERKA.Services.ViewModels;

namespace INZYNIERKA.Services.Interfaces
{
    public interface IChatService
    {
        Task<ChatViewModel> GetPrivateChatAsync(string currentUserId, string friendId, string userMessage, string geminiAnswer);
        Task<List<MessageViewModel>> GetOlderPrivateMessagesAsync(string currentUserId, string friendId, int skip, int take = 30);
        Task<GroupChatViewModel> GetGroupChatAsync(string currentUserId, int groupId, string userMessage, string geminiAnswer);
        Task<List<GroupMessageViewModel>> GetOlderGroupMessagesAsync(int groupId, int skip, int take = 30);
        Task SavePrivateMessageAsync(string senderId, string receiverId, string content);
        Task<bool> SaveImageMessageAsync(string senderId, string receiverId, byte[] imageData, string imageType);
        Task SaveGroupMessageAsync(int groupId, string senderId, string content);
        Task<bool> SaveGroupImageMessageAsync(string senderId, int groupId, byte[] imageData, string imageType);
        Task ClearMessageNotificationAsync(string userId, string friendId);
    }
}