using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.ViewModels;

namespace INZYNIERKA.Services.Interfaces
{
    public interface IChatService<TUser> where TUser : User
    {
        Task<ChatViewModel> Chat(string currentUserId, string friendId, string userMessage, string geminiAnswer);
        Task<List<MessageViewModel>> OlderMessages(string currentUserId, string friendId, int skip, int take = 30);
        Task<string> SaveMessage(string senderId, string receiverId, string content, bool autoTranslate);
        Task<bool> SaveImage(string senderId, string receiverId, byte[] imageData, string imageType);
        Task ClearNotification(string userId, string friendId);
        Task MarkAsReaded(string userId, string friendId);

        Task<GroupChatViewModel> GroupChat(string currentUserId, int groupId, string userMessage, string geminiAnswer);
        Task<List<GroupMessageViewModel>> OlderGroupMessages(int groupId, int skip, int take = 30);
        Task<string> SaveGroupMessage(int groupId, string senderId, string content);
        Task<bool> SaveGroupImage(string senderId, int groupId, byte[] imageData, string imageType);
        Task ClearGroupNotification(string userId, int groupId);
    }
}