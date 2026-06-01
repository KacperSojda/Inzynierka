using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.ViewModels;

namespace INZYNIERKA.Services.Interfaces
{
    public interface IGroupMemberService<TUser> where TUser : User
    {
        Task<GroupMembersViewModel> GroupMembers(int groupId, string currentUserId);
        Task<bool> GiveAdmin(int groupId, string targetUserId, string currentUserId);
        Task<bool> DemoteAdmin(int groupId, string targetUserId, string currentUserId);
        Task<bool> KickUser(int groupId, string targetUserId, string currentUserId);
        Task<bool> BanUser(int groupId, string targetUserId, string currentUserId);
    }
}