using INZYNIERKA.ViewModels;

namespace INZYNIERKA.Services
{
    public interface IFriendshipService
    {
        Task<bool> AcceptFriendRequestAsync(string currentUserId, int notificationId);
        Task<List<FriendViewModel>> GetFriendListAsync(string userId);
        Task DeleteFriendAsync(string currentUserId, string friendId);
        Task<List<FriendViewModel>> GetRequestListAsync(string userId);
        Task DeleteRequestAsync(string currentUserId, string friendId);
    }
}