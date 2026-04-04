using INZYNIERKA.Data;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services
{
    public class ChatAiService : IChatAiService
    {
        private readonly INZDbContext context;
        private readonly IGeminiService geminiService;
        private readonly IConfiguration configuration;

        public ChatAiService(INZDbContext context, IGeminiService geminiService, IConfiguration configuration)
        {
            this.context = context;
            this.geminiService = geminiService;
            this.configuration = configuration;
        }

        public async Task<string> GetPrivateResponseHelpAsync(string currentUserId, string friendId)
        {
            var messages = await context.Messages.Include(m => m.Sender)
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == friendId) || (m.SenderId == friendId && m.ReceiverId == currentUserId))
                .OrderByDescending(m => m.DateTime).Take(30).ToListAsync();

            messages.Reverse();
            var messageString = string.Join(", ", messages.Select(m => (m.SenderId == currentUserId ? "[user]" : $"[{m.Sender.UserName}]") + " " + m.Content));
            return await geminiService.AskAsync(messageString, configuration["Prompts:ResponseHelp"]);
        }

        public async Task<string> GetGroupResponseHelpAsync(string currentUserId, int groupId)
        {
            var messages = await context.GroupMessages.Include(m => m.Sender)
                .Where(m => m.GroupId == groupId).OrderByDescending(m => m.Timestamp).Take(30).ToListAsync();

            var messageString = string.Join(", ", messages.Select(m => (m.SenderId == currentUserId ? "[user]" : $"[{m.Sender.UserName}]") + " " + m.Content));
            return await geminiService.AskAsync(messageString, configuration["Prompts:ResponseHelp"]);
        }

        public async Task<string> CorrectMessageAsync(string userMessage)
        {
            return await geminiService.AskAsync(userMessage, configuration["Prompts:CorrectMessage"]);
        }

        public async Task<string> TranslatePrivateMessageAsync(string currentUserId, string friendId, string userMessage)
        {
            var messages = await context.Messages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == friendId) || (m.SenderId == friendId && m.ReceiverId == currentUserId))
                .OrderByDescending(m => m.DateTime).Take(30).Select(m => m.Content).ToListAsync();

            messages.Reverse();
            string messagesToString = string.Join(", ", messages);

            string language = await geminiService.AskAsync(messagesToString, configuration["Prompts:Language"]);
            string translatePrompt = configuration["Prompts:Translate"].Replace("{language}", language);

            return await geminiService.AskAsync(userMessage, translatePrompt);
        }

        public async Task<string> TranslateGroupMessageAsync(int groupId, string userMessage)
        {
            var messages = await context.GroupMessages
                .Where(m => m.GroupId == groupId).OrderByDescending(m => m.Timestamp).Take(30).Select(m => m.Content).ToListAsync();

            string messagesToString = string.Join(", ", messages);

            string language = await geminiService.AskAsync(messagesToString, configuration["Prompts:Language"]);
            string translatePrompt = configuration["Prompts:Translate"].Replace("{language}", language);

            return await geminiService.AskAsync(userMessage, translatePrompt);
        }

        public async Task<string> CensorMessageAsync(string message)
        {
            string cenzurePrompt = configuration["Prompts:Cenzure"];
            return await geminiService.AskAsync(message, cenzurePrompt);
        }
    }
}