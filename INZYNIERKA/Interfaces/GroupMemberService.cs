using INZYNIERKA.ViewModels;

namespace INZYNIERKA.Services
{
    public interface IGroupMemberService
    {
        Task<GroupMembersViewModel> GetGroupMembersAsync(int groupId, string currentUserId);
        Task<bool> GiveAdminAsync(int groupId, string targetUserId, string currentUserId);
        Task<bool> DemoteAdminAsync(int groupId, string targetUserId, string currentUserId);
        Task<bool> KickUserAsync(int groupId, string targetUserId, string currentUserId);
        Task<bool> BanUserAsync(int groupId, string targetUserId, string currentUserId);
    }
}