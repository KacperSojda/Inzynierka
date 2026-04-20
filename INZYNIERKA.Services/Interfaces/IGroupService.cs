using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.ViewModels;

namespace INZYNIERKA.Services.Interfaces
{
    public interface IGroupService
    {
        Task<GroupViewModel> GetAvailableGroupsAsync(string userId);
        Task<GroupViewModel> GetUserGroupsAsync(string userId);
        Task CreateGroupAsync(string name, string creatorUserId);
        Task JoinGroupAsync(int groupId, string userId);
        Task LeaveGroupAsync(int groupId, string userId);
        Task<Group> GetGroupForEditAsync(int groupId, string currentUserId);
        Task UpdateGroupAsync(Group model, string currentUserId);
        Task DeleteGroupAsync(int groupId, string currentUserId);
        Task<SelectGroupTagsViewModel> GetGroupTagsForSelectionAsync(int groupId, string currentUserId);
        Task UpdateGroupTagsAsync(int groupId, string currentUserId, List<int> selectedTagIds);
    }
}