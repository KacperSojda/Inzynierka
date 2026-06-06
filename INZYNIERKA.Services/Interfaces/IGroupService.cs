using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.ViewModels;

namespace INZYNIERKA.Services.Interfaces
{
    public interface IGroupService<TUser> where TUser : User
    {
        Task<(GroupViewModel Model, int TotalCount)> AvailableGroups(string userId, string? searchQuery = null, int page = 1, int pageSize = 10);
        Task<(GroupViewModel Model, int TotalCount)> UserGroups(string userId, string? searchQuery = null, int page = 1, int pageSize = 10);
        Task CreateGroup(string name, string creatorUserId);
        Task JoinGroup(int groupId, string userId);
        Task LeaveGroup(int groupId, string userId);
        Task<EditGroupViewModel> EditGroup(int groupId, string currentUserId);
        Task UpdateGroup(EditGroupViewModel model, string currentUserId);
        Task DeleteGroup(int groupId, string currentUserId);
        Task<SelectGroupTagsViewModel> GroupTags(int groupId, string currentUserId);
        Task UpdateGroupTags(int groupId, string currentUserId, List<int> selectedTagIds);
    }
}