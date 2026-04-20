using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.ViewModels;

namespace INZYNIERKA.Services.Interfaces    
{
    public interface ITagService
    {
        Task<SelectTagsViewModel> GetUserTagsForSelectionAsync(string userId);
        Task UpdateUserTagsAsync(string userId, List<int> selectedTagIds);
        Task AddNewTagAsync(string tagName);
        Task<List<Tag>> GetAllTagsAsync();
    }
}