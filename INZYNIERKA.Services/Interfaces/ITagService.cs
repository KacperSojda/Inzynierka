using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.ViewModels;

namespace INZYNIERKA.Services.Interfaces    
{
    public interface ITagService<TUser> where TUser : User
    {
        Task<SelectTagsViewModel> UserTags(string userId);
        Task UpdateUserTags(string userId, List<int> selectedTagIds);
        Task<(bool Result, string ErrorMessage)> NewTag(string tagName);
        Task<List<Tag>> AllTags();
    }
}