using INZYNIERKA.Models;
using INZYNIERKA.ViewModels;

namespace INZYNIERKA.Services
{
    public interface ITagService
    {
        Task<SelectTagsViewModel> GetUserTagsForSelectionAsync(string userId);
        Task UpdateUserTagsAsync(string userId, List<int> selectedTagIds);
        Task AddNewTagAsync(string tagName);
        Task<List<Tag>> GetAllTagsAsync();
    }
}