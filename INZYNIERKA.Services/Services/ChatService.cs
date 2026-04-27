using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Web;

namespace INZYNIERKA.Services.Services
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
                    DateTime = m.DateTime,
                    ImageDataBase64 = m.ImageData != null ? Convert.ToBase64String(m.ImageData) : null,
                    ImageType = m.ImageType
                }).ToList(),
                UserMessage = userMessage,
                GeminiAnswer = geminiAnswer,
                GeminiQuestion = "",
            };
        }

        public async Task<List<MessageViewModel>> GetOlderPrivateMessagesAsync(string userId, string friendId, int skip, int take = 30)
        {
            var messages = await context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => (m.SenderId == userId && m.ReceiverId == friendId) ||
                            (m.SenderId == friendId && m.ReceiverId == userId))
                .OrderByDescending(m => m.DateTime)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            messages.Reverse();

            return messages.Select(m => new MessageViewModel
            {
                SenderId = m.SenderId,
                SenderName = m.Sender.UserName,
                ReceiverId = m.ReceiverId,
                ReceiverName = m.Receiver.UserName,
                Content = m.Content,
                DateTime = m.DateTime,
                ImageDataBase64 = m.ImageData != null ? Convert.ToBase64String(m.ImageData) : null,
                ImageType = m.ImageType
            }).ToList();
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
                    DateTime = m.Timestamp,
                    ImageDataBase64 = m.ImageData != null ? Convert.ToBase64String(m.ImageData) : null,
                    ImageType = m.ImageType
                }).ToList(),
                UserMessage = userMessage,
                GeminiAnswer = geminiAnswer
            };
        }

        public async Task<List<GroupMessageViewModel>> GetOlderGroupMessagesAsync(int groupId, int skip, int take = 30)
        {
            var messages = await context.GroupMessages
                .Include(m => m.Sender)
                .Where(m => m.GroupId == groupId)
                .OrderByDescending(m => m.Timestamp)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            messages.Reverse();

            return messages.Select(m => new GroupMessageViewModel
            {
                SenderId = m.SenderId,
                SenderName = m.Sender.UserName,
                Content = m.Content,
                DateTime = m.Timestamp,
                ImageDataBase64 = m.ImageData != null ? Convert.ToBase64String(m.ImageData) : null,
                ImageType = m.ImageType
            }).ToList();
        }

        public async Task SavePrivateMessageAsync(string senderId, string receiverId, string content)
        {
            var msg = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                DateTime = DateTime.UtcNow
            };

            context.Messages.Add(msg);

            var existingNotification = await context.Notifications
                .FirstOrDefaultAsync(n => n.SenderId == senderId && n.ReceiverId == receiverId && n.Type == NotificationType.Message);

            if (existingNotification == null)
            {
                var notification = new Notification
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Type = NotificationType.Message,
                    CreationDate = DateTime.UtcNow
                };

                context.Notifications.Add(notification);
            }

            await context.SaveChangesAsync();
        }

        public async Task<bool> SaveImageMessageAsync(string senderId, string receiverId, byte[] imageData, string imageType)
        {
            try
            {
                if (imageData == null || imageData.Length == 0)
                    return false;

                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = null,
                    ImageData = imageData,
                    ImageType = imageType,
                    DateTime = DateTime.UtcNow,

                };

                context.Messages.Add(message);
                await context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task SaveGroupMessageAsync(int groupId, string senderId, string content)
        {

            var group = await context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return;

            var groupMessage = new GroupMessage
            {
                GroupId = groupId,
                SenderId = senderId,
                Content = content,
                Timestamp = DateTime.UtcNow
            };

            context.GroupMessages.Add(groupMessage);

            foreach (var member in group.Members)
            {
                if (member.UserId == senderId) continue;

                var existingNotification = await context.Notifications.FirstOrDefaultAsync(n =>
                    n.GroupId == groupId &&
                    n.ReceiverId == member.UserId &&
                    n.Type == NotificationType.GroupMessage);

                if (existingNotification == null)
                {
                    var notification = new Notification
                    {
                        SenderId = senderId,
                        GroupId = groupId,
                        ReceiverId = member.UserId,
                        Type = NotificationType.GroupMessage,
                        CreationDate = DateTime.UtcNow
                    };

                    context.Notifications.Add(notification);
                }
            }

            await context.SaveChangesAsync();
        }

        public async Task<bool> SaveGroupImageMessageAsync(string senderId, int groupId, byte[] imageData, string imageType)
        {
            try
            {
                if (imageData == null || imageData.Length == 0)
                    return false;

                var message = new GroupMessage
                {

                    GroupId = groupId,
                    SenderId = senderId,
                    Content = null,
                    ImageData = imageData,
                    ImageType = imageType,
                    Timestamp = DateTime.UtcNow,

                };

                context.GroupMessages.Add(message);
                await context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task ClearMessageNotificationAsync(string userId, string friendId)
        {
            var notification = await context.Notifications
                .FirstOrDefaultAsync(n => n.SenderId == friendId && n.ReceiverId == userId);

            if (notification != null)
            {
                context.Notifications.Remove(notification);
                await context.SaveChangesAsync();
            }
        }
    }
}