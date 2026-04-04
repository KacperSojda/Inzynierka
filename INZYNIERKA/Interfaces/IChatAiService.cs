namespace INZYNIERKA.Services
{
    public interface IChatAiService
    {
        Task<string> GetPrivateResponseHelpAsync(string currentUserId, string friendId);
        Task<string> GetGroupResponseHelpAsync(string currentUserId, int groupId);

        Task<string> CorrectMessageAsync(string userMessage);

        Task<string> TranslatePrivateMessageAsync(string currentUserId, string friendId, string userMessage);
        Task<string> TranslateGroupMessageAsync(int groupId, string userMessage);

        Task<string> CensorMessageAsync(string message);
    }
}