using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace INZYNIERKA.Services.Services
{
    public class ChatAiService<TUser> : IChatAiService<TUser> where TUser : User
    {
        private readonly INZDbContext<TUser> _context;
        private readonly IGeminiService _geminiService;
        private readonly IConfiguration _configuration;

        public ChatAiService(INZDbContext<TUser> context, IGeminiService geminiService, IConfiguration configuration)
        {
            _context = context;
            _geminiService = geminiService;
            _configuration = configuration;
        }

        public async Task<List<string>> ResponseHelp(string currentUserId, string friendId)
        {
            var relation = await _context.UserFriends
                .FirstOrDefaultAsync(f => f.UserId == currentUserId && f.FriendId == friendId);

            string tone = relation?.Tone ?? "casual";
            string? custom = relation?.Custom;

            var messages = await _context.Messages.Include(m => m.Sender)
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == friendId) ||
                            (m.SenderId == friendId && m.ReceiverId == currentUserId))
                .OrderByDescending(m => m.Timestamp)
                .Take(30)
                .ToListAsync();

            messages.Reverse();

            var historyBuilder = new StringBuilder();
            foreach (var msg in messages.OrderBy(m => m.Timestamp))
            {
                string senderLabel = msg.SenderId == currentUserId ? "User" : "Friend";
                historyBuilder.AppendLine($"{senderLabel}: {msg.Content}");
            }
            string chatHistory = historyBuilder.ToString();

            string styleInstruction = _configuration["Prompts:StyleBase"];

            if (tone == "custom" && !string.IsNullOrWhiteSpace(custom))
            {
                string customprompt = _configuration["Prompts:Custom"];
                styleInstruction = $"{customprompt}'{custom}'.";
            }
            else if (tone == "casual")
            {
                styleInstruction = _configuration["Prompts:Casual"];
            }
            else if (tone == "formal")
            {
                styleInstruction = _configuration["Prompts:Formal"];
            }
            else if (tone == "funny")
            {
                styleInstruction = _configuration["Prompts:Funny"];
            }

            string basePrompt = _configuration["Prompts:ResponseHelp"];
            string fullSystemPrompt = $"{basePrompt}{styleInstruction}\n\nCHAT HISTORY:";

            var ans = await _geminiService.AskAsync(chatHistory, fullSystemPrompt);

            if (string.IsNullOrWhiteSpace(ans))
            {
                return new List<string>();
            }

            return ans.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(3).ToList();
        }

        public async Task SaveSRSettings(string currentUserId, string friendId, string tone, string custom, bool auto)
        {
            var relation = await _context.UserFriends
                .FirstOrDefaultAsync(f => f.UserId == currentUserId && f.FriendId == friendId);

            if (relation != null)
            {
                relation.Tone = tone;
                relation.Custom = custom;
                relation.SmartReplies = auto;

                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<string>> GroupResponseHelp(string currentUserId, int groupId)
        {
            var relation = await _context.UserGroups
                .FirstOrDefaultAsync(ug => ug.UserId == currentUserId && ug.ChatGroupId == groupId);

            string tone = relation?.Tone ?? "casual";
            string? custom = relation?.Custom;

            var messages = await _context.GroupMessages.Include(m => m.Sender)
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

            string styleInstruction = _configuration["Prompts:StyleBase"];
            if (tone == "custom" && !string.IsNullOrWhiteSpace(custom))
            {
                string customprompt = _configuration["Prompts:Custom"];
                styleInstruction = $"{customprompt}'{custom}'.";
            }
            else if (tone == "casual")
            {
                styleInstruction = _configuration["Prompts:Casual"];
            }
            else if (tone == "formal")
            {
                styleInstruction = _configuration["Prompts:Formal"];
            }
            else if (tone == "funny")
            {
                styleInstruction = _configuration["Prompts:Funny"];
            }

            string basePrompt = _configuration["Prompts:ResponseHelp"];
            string fullSystemPrompt = $"{basePrompt}{styleInstruction}\n\nCHAT HISTORY:";

            var ans = await _geminiService.AskAsync(chatHistory, fullSystemPrompt);

            if (string.IsNullOrWhiteSpace(ans))
            {
                return new List<string>();
            }

            return ans.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(3).ToList();
        }

        public async Task<string> CorrectMessage(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage)) return userMessage;

            var correctedMessage = await _geminiService.AskAsync(userMessage, _configuration["Prompts:CorrectMessage"]);

            return string.IsNullOrEmpty(correctedMessage) ? userMessage : correctedMessage;

        }

        public async Task<string> TranslateMessage(string currentUserId, string friendId, string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage)) return userMessage;

            var messages = await _context.Messages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == friendId) || (m.SenderId == friendId && m.ReceiverId == currentUserId))
                .OrderByDescending(m => m.Timestamp).Take(30).Select(m => m.Content).ToListAsync();

            messages.Reverse();
            string language = "English";
            if (messages.Any())
            {
                string messagesToString = string.Join(", ", messages);
                language = await _geminiService.AskAsync(messagesToString, _configuration["Prompts:Language"]) ?? "English";
            }

            string translatePrompt = _configuration["Prompts:Translate"].Replace("{language}", language);

            var translatedResult = await _geminiService.AskAsync(userMessage, translatePrompt);

            return string.IsNullOrEmpty(translatedResult) ? userMessage : translatedResult;
        }

        public async Task<string> AutoTranslateToUserLanguage(string targetUserId, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return message;

            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == targetUserId);

            string targetLanguage = string.IsNullOrWhiteSpace(targetUser?.PreferredLanguages)
                ? "English"
                : targetUser.PreferredLanguages;

            string translatePrompt = _configuration["Prompts:Translate"].Replace("{language}", targetLanguage);

            var translatedResult = await _geminiService.AskAsync(message, translatePrompt);

            return string.IsNullOrEmpty(translatedResult) ? message : translatedResult;
        }

        public async Task<string> TranslateGroupMessage(int groupId, string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage)) return userMessage;

            var messages = await _context.GroupMessages
                .Where(m => m.GroupId == groupId).OrderByDescending(m => m.Timestamp).Take(30).Select(m => m.Content).ToListAsync();

            messages.Reverse();

            string language = "English";

            if (messages.Any())
            {
                string messagesToString = string.Join(", ", messages);
                language = await _geminiService.AskAsync(messagesToString, _configuration["Prompts:Language"]) ?? "English";
            }

            string translatePrompt = _configuration["Prompts:Translate"].Replace("{language}", language);

            var translatedResult = await _geminiService.AskAsync(userMessage, translatePrompt);
            return string.IsNullOrEmpty(translatedResult) ? userMessage : translatedResult;
        }

        public async Task<string> CensorMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return message;
            }

            string censorPrompt = _configuration["Prompts:Censor"];
            var censoredMessage = await _geminiService.AskAsync(message, censorPrompt);
            return string.IsNullOrEmpty(censoredMessage) ? message : censoredMessage;
        }

        public async Task<string> SummarizeChat(string currentUserId, string friendId, DateTime startDate, DateTime endDate)
        {
            var messages = await _context.Messages.Include(m => m.Sender)
                .Where(m => ((m.SenderId == currentUserId && m.ReceiverId == friendId) ||
                             (m.SenderId == friendId && m.ReceiverId == currentUserId))
                             && m.Timestamp.Date >= startDate.Date
                             && m.Timestamp.Date <= endDate.Date)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            if (!messages.Any()) return "No messages in the selected period.";

            var historyBuilder = new StringBuilder();
            foreach (var msg in messages)
            {
                string senderLabel = msg.SenderId == currentUserId ? "User" : "Friend";
                if (!string.IsNullOrWhiteSpace(msg.Content))
                    historyBuilder.AppendLine($"{senderLabel}: {msg.Content}");
            }

            var prompt = _configuration["Prompts:SummarizeChat"];
            var summary = await _geminiService.AskAsync(historyBuilder.ToString(), prompt);

            return string.IsNullOrEmpty(summary) ? "AI was unable to generate a summary." : summary;
        }

        public async Task<string> SummarizeGroupChat(string currentUserId, int groupId, DateTime startDate, DateTime endDate)
        {
            var messages = await _context.GroupMessages.Include(m => m.Sender)
                .Where(m => m.GroupId == groupId
                             && m.Timestamp.Date >= startDate.Date
                             && m.Timestamp.Date <= endDate.Date)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            if (!messages.Any()) return "No messages in the selected period.";

            var historyBuilder = new StringBuilder();
            foreach (var msg in messages)
            {
                string senderLabel = msg.SenderId == currentUserId ? "User" : msg.Sender.UserName;

                if (!string.IsNullOrWhiteSpace(msg.Content))
                    historyBuilder.AppendLine($"{senderLabel}: {msg.Content}");
            }

            var prompt = _configuration["Prompts:SummarizeChat"];
            var summary = await _geminiService.AskAsync(historyBuilder.ToString(), prompt);

            return string.IsNullOrEmpty(summary) ? "AI was unable to generate a summary." : summary;
        }

        public async Task SaveGroupSRSettings(string currentUserId, int groupId, string tone, string custom, bool auto)
        {
            var relation = await _context.UserGroups
                .FirstOrDefaultAsync(ug => ug.UserId == currentUserId && ug.ChatGroupId == groupId);

            if (relation != null)
            {
                relation.Tone = tone;
                relation.Custom = custom;
                relation.SmartReplies = auto;

                await _context.SaveChangesAsync();
            }
        }
    }
}