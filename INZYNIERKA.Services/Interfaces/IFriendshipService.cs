using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.ViewModels;

namespace INZYNIERKA.Services.Interfaces
{
    public interface IFriendshipService<TUser> where TUser : User
    {
        Task<bool> AcceptRequest(string currentUserId, int notificationId);
        Task<(List<FriendViewModel> Friends, int TotalCount)> FriendList(string userId, string? searchQuery, int page, int pageSize);
        Task DeleteFriend(string currentUserId, string friendId);
        Task<(List<FriendViewModel> Requests, int TotalCount)> RequestList(string userId, int page, int pageSize);
        Task DeleteRequest(string currentUserId, string friendId);
        Task SendRequest(string senderId, string receiverId);
    }
}