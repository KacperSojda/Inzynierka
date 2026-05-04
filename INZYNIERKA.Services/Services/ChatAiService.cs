using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace INZYNIERKA.Services.Services
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

            var historyBuilder = new StringBuilder();
            foreach (var msg in messages.OrderBy(m => m.DateTime))
            {
                string senderLabel = msg.SenderId == currentUserId ? "User" : "Friend";
                historyBuilder.AppendLine($"{senderLabel}: {msg.Content}");
            }

            string chatHistory = historyBuilder.ToString();

            var ans =  await geminiService.AskAsync(chatHistory, configuration["Prompts:ResponseHelp"]);

            return string.IsNullOrEmpty(ans) ? string.Empty : ans;
        }

        public async Task<string> GetGroupResponseHelpAsync(string currentUserId, int groupId)
        {
            var messages = await context.GroupMessages.Include(m => m.Sender)
                .Where(m => m.GroupId == groupId)
                .OrderByDescending(m => m.Timestamp)
                .Take(30)
                .ToListAsync();

            messages.Reverse();

            var historyBuilder = new StringBuilder();
            foreach (var msg in messages.OrderBy(m => m.Timestamp))
            {
                string senderLabel = msg.SenderId == currentUserId ? "User" : msg.Sender.UserName;
                historyBuilder.AppendLine($"{senderLabel}: {msg.Content}");
            }

            string chatHistory = historyBuilder.ToString();

            var ans = await geminiService.AskAsync(chatHistory, configuration["Prompts:ResponseHelp"]);

            return string.IsNullOrEmpty(ans) ? string.Empty : ans;
        }

        public async Task<string> CorrectMessageAsync(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage)) return userMessage;

            var correctedMessage = await geminiService.AskAsync(userMessage, configuration["Prompts:CorrectMessage"]);

            return string.IsNullOrEmpty(correctedMessage) ? userMessage : correctedMessage;

        }

        public async Task<string> TranslatePrivateMessageAsync(string currentUserId, string friendId, string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage)) return userMessage;

            var messages = await context.Messages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == friendId) || (m.SenderId == friendId && m.ReceiverId == currentUserId))
                .OrderByDescending(m => m.DateTime).Take(30).Select(m => m.Content).ToListAsync();

            messages.Reverse();
            string language = "English";
            if (messages.Any())
            {
                string messagesToString = string.Join(", ", messages);
                language = await geminiService.AskAsync(messagesToString, configuration["Prompts:Language"]) ?? "English";
            }

            string translatePrompt = configuration["Prompts:Translate"].Replace("{language}", language);

            var translatedResult = await geminiService.AskAsync(userMessage, translatePrompt);

            return string.IsNullOrEmpty(translatedResult) ? userMessage : translatedResult;
        }

        public async Task<string> TranslateGroupMessageAsync(int groupId, string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage)) return userMessage;

            var messages = await context.GroupMessages
                .Where(m => m.GroupId == groupId).OrderByDescending(m => m.Timestamp).Take(30).Select(m => m.Content).ToListAsync();

            messages.Reverse();

            string language = "English";

            if (messages.Any())
            {
                string messagesToString = string.Join(", ", messages);
                language = await geminiService.AskAsync(messagesToString, configuration["Prompts:Language"]) ?? "English";
            }

            string translatePrompt = configuration["Prompts:Translate"].Replace("{language}", language);

            var translatedResult = await geminiService.AskAsync(userMessage, translatePrompt);
            return string.IsNullOrEmpty(translatedResult) ? userMessage : translatedResult;
        }

        public async Task<string> CensorMessageAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return message;
            }

            string censorPrompt = configuration["Prompts:Censor"];
            return await geminiService.AskAsync(message, censorPrompt);
        }
    }
}