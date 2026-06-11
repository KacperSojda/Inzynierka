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

        /// <summary>Generates smart replies for a private chat based on recent history and user tone settings.</summary>
        /// <param name="currentUserId">The ID of the current user.</param>
        /// <param name="friendId">The ID of the friend.</param>
        /// <returns>A list of up to three suggested replies.</returns>
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

        /// <summary>Saves the user's smart reply configuration for a specific private chat.</summary>
        /// <param name="currentUserId">The ID of the current user.</param>
        /// <param name="friendId">The ID of the friend.</param>
        /// <param name="tone">The selected AI response tone.</param>
        /// <param name="custom">Custom instructions for the AI response style.</param>
        /// <param name="auto">Indicates whether smart replies should be generated automatically.</param>
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

        /// <summary>Generates smart replies for a group chat based on recent history and user tone settings.</summary>
        /// <param name="currentUserId">The ID of the current user.</param>
        /// <param name="groupId">The ID of the group chat.</param>
        /// <returns>A list of up to three suggested replies.</returns>
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

        /// <summary>Uses AI to correct grammar in the provided message.</summary>
        /// <param name="userMessage">The message to be corrected.</param>
        /// <returns>The corrected message, or the original if no changes were made.</returns>
        public async Task<string> CorrectMessage(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage)) return userMessage;

            var correctedMessage = await _geminiService.AskAsync(userMessage, _configuration["Prompts:CorrectMessage"]);

            return string.IsNullOrEmpty(correctedMessage) ? userMessage : correctedMessage;

        }
        
        /// <summary>Translates a message between two users with AI.</summary>
        /// <param name="currentUserId">The ID of the current user.</param>
        /// <param name="friendId">The ID of the friend.</param>
        /// <param name="userMessage">The message to be translated.</param>
        /// <returns>The translated message, or the original if no translation was made.</returns>
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

        /// <summary>Translates a message into the target user's preferred language.</summary>
        /// <param name="targetUserId">The ID of the user who will receive the message.</param>
        /// <param name="message">The message to be translated.</param>
        /// <returns>The translated message.</returns>
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

        /// <summary>Detects the context language of a group chat and translates the message into it.</summary>
        /// <param name="groupId">The ID of the group chat.</param>
        /// <param name="userMessage">The message to be translated.</param>
        /// <returns>The translated message.</returns>
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

        /// <summary>Uses AI to detect and censor content in a message.</summary>
        /// <param name="message">The message to be analyzed.</param>
        /// <returns>The censored message, or the original if no inappropriate content was found.</returns>
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

        /// <summary>Generates an AI summary of a private chat history within a specified date range.</summary>
        /// <param name="currentUserId">The ID of the current user.</param>
        /// <param name="friendId">The ID of the friend.</param>
        /// <param name="startDate">The start date of the summary.</param>
        /// <param name="endDate">The end date of the summary.</param>
        /// <returns>A text summary of the conversation.</returns>
        public async Task<string> SummarizeChat(string currentUserId, string friendId, DateTime startDate, DateTime endDate)
        {
            DateTime utcStart = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
            DateTime utcEnd = DateTime.SpecifyKind(endDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

            var messages = await _context.Messages.Include(m => m.Sender)
                .Where(m => ((m.SenderId == currentUserId && m.ReceiverId == friendId) ||
                             (m.SenderId == friendId && m.ReceiverId == currentUserId))
                             && m.Timestamp.Date >= utcStart
                             && m.Timestamp.Date <= utcEnd)
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

        /// <summary>Generates an AI summary of a group chat history within a specified date range.</summary>
        /// <param name="currentUserId">The ID of the current user.</param>
        /// <param name="groupId">The ID of the group chat.</param>
        /// <param name="startDate">The start date of the summary.</param>
        /// <param name="endDate">The end date of the summary.</param>
        /// <returns>A text summary of the group conversation.</returns>
        public async Task<string> SummarizeGroupChat(string currentUserId, int groupId, DateTime startDate, DateTime endDate)
        {
            DateTime utcStart = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
            DateTime utcEnd = DateTime.SpecifyKind(endDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

            var messages = await _context.GroupMessages.Include(m => m.Sender)
                .Where(m => m.GroupId == groupId
                             && m.Timestamp.Date >= utcStart
                             && m.Timestamp.Date <= utcEnd)
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

        /// <summary>Saves the user's smart reply configuration for a specific group chat.</summary>
        /// <param name="currentUserId">The ID of the current user.</param>
        /// <param name="groupId">The ID of the group chat.</param>
        /// <param name="tone">The selected AI response tone.</param>
        /// <param name="custom">Custom instructions for the AI response style.</param>
        /// <param name="auto">Indicates whether smart replies should be generated automatically.</param>
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