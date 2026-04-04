using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Services
{
    public class ChatService : IChatService
    {
        private readonly INZDbContext context;
        private readonly UserManager<User> userManager;

        public ChatService(INZDbContext context, UserManager<User> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }

        public async Task<ChatViewModel> GetPrivateChatAsync(string currentUserId, string friendId, string userMessage, string geminiAnswer)
        {
            var user = await userManager.FindByIdAsync(currentUserId);
            var friend = await userManager.FindByIdAsync(friendId);

            if (user == null || friend == null) return null;

            var messages = await context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == friendId) ||
                            (m.SenderId == friendId && m.ReceiverId == currentUserId))
                .OrderByDescending(m => m.DateTime)
                .Take(30)
                .ToListAsync();

            messages.Reverse();

            return new ChatViewModel
            {
                FriendId = friend.Id,
                FriendName = friend.UserName,
                CurrentUserId = user.Id,
                CurrentUserName = user.UserName,
                Messages = messages.Select(m => new MessageViewModel
                {
                    SenderId = m.SenderId,
                    SenderName = m.Sender.UserName,
                    ReceiverId = m.ReceiverId,
                    ReceiverName = m.Receiver.UserName,
                    Content = m.Content,
                    DateTime = m.DateTime
                }).ToList(),
                UserMessage = userMessage,
                GeminiAnswer = geminiAnswer,
                GeminiQuestion = "",
            };
        }

        public async Task<GroupChatViewModel> GetGroupChatAsync(string currentUserId, int groupId, string userMessage, string geminiAnswer)
        {
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return null;

            var isMember = await context.UserGroups.AnyAsync(ug => ug.ChatGroupId == groupId && ug.UserId == currentUserId);
            if (!isMember) throw new UnauthorizedAccessException("Nie należysz do tej grupy.");

            var messages = await context.GroupMessages
                .Include(m => m.Sender)
                .Where(m => m.GroupId == groupId)
                .OrderByDescending(m => m.Timestamp)
                .Take(30)
                .ToListAsync();

            messages.Reverse();

            return new GroupChatViewModel
            {
                groupID = groupId,
                groupName = group.Name,
                currentUserID = currentUserId,
                messages = messages.Select(m => new GroupMessageViewModel
                {
                    SenderId = m.SenderId,
                    SenderName = m.Sender.UserName,
                    Content = m.Content,
                    DateTime = m.Timestamp
                }).ToList(),
                UserMessage = userMessage,
                GeminiAnswer = geminiAnswer
            };
        }
    }
}