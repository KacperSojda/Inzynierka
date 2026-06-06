using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.ViewModels;

namespace INZYNIERKA.Services.Interfaces
{
    public interface IChatAiService<TUser> where TUser : User
    {
        Task<List<string>> ResponseHelp(string currentUserId, string friendId);
        Task SaveSRSettings(string currentUserId, string friendId, string tone, string custom, bool auto);
        Task<string> SummarizeChat(string currentUserId, string friendId, DateTime startDate, DateTime endDate);


        Task<List<string>> GroupResponseHelp(string currentUserId, int groupId);
        Task SaveGroupSRSettings(string currentUserId, int groupId, string tone, string custom, bool auto);
        Task<string> SummarizeGroupChat(string currentUserId, int groupId, DateTime startDate, DateTime endDate);

        Task<string> CensorMessage(string message);
        Task<string> CorrectMessage(string userMessage);
        Task<string> AutoTranslateToUserLanguage(string targetUserId, string message);
        Task<string> TranslateMessage(string currentUserId, string friendId, string userMessage);
        Task<string> TranslateGroupMessage(int groupId, string userMessage);

        
    }
}