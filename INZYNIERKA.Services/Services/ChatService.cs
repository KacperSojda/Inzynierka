using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace INZYNIERKA.Services.Services
{
    public class ChatService<TUser> : IChatService<TUser> where TUser : User
    {
        private readonly INZDbContext<TUser> context;
        private readonly UserManager<TUser> userManager;
        private readonly SignInManager<TUser> signInManager;
        private readonly IChatAiService<TUser> chatAiService;

        public ChatService(INZDbContext<TUser> context, UserManager<TUser> userManager, SignInManager<TUser> signInManager, IChatAiService<TUser> chatAiService)
        {
            this.context = context;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.chatAiService = chatAiService;
        }

        public async Task<ChatViewModel> Chat(string currentUserId, string friendId, string userMessage, string geminiAnswer)
        {
            var user = await userManager.FindByIdAsync(currentUserId);
            var friend = await userManager.FindByIdAsync(friendId);

            if (user == null || friend == null) return null;

            var messages = await context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == friendId) ||
                            (m.SenderId == friendId && m.ReceiverId == currentUserId))
                .OrderByDescending(m => m.Timestamp)
                .Take(30)
                .ToListAsync();

            messages.Reverse();

            var relation = await context.UserFriends
                .FirstOrDefaultAsync(f => f.UserId == currentUserId && f.FriendId == friendId);

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
                    Timestamp = m.Timestamp,
                    ImageData = m.ImageData != null ? Convert.ToBase64String(m.ImageData) : null,
                    ImageType = m.ImageType,
                    Readed = m.Readed
                }).ToList(),
                UserMessage = userMessage,
                GeminiAnswer = geminiAnswer,
                GeminiQuestion = "",
                Tone = relation?.Tone ?? "casual",
                Custom = relation?.Custom,
                Auto = relation?.SmartReplies ?? true
            };
        }

        public async Task<List<MessageViewModel>> OlderMessages(string userId, string friendId, int skip, int take = 30)
        {
            var messages = await context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => (m.SenderId == userId && m.ReceiverId == friendId) ||
                            (m.SenderId == friendId && m.ReceiverId == userId))
                .OrderByDescending(m => m.Timestamp)
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
                Timestamp = m.Timestamp,
                ImageData = m.ImageData != null ? Convert.ToBase64String(m.ImageData) : null,
                ImageType = m.ImageType
            }).ToList();
        }

        public async Task<GroupChatViewModel> GroupChat(string currentUserId, int groupId, string userMessage, string geminiAnswer)
        {
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return null;

            var isMember = await context.UserGroups.AnyAsync(ug => ug.ChatGroupId == groupId && ug.UserId == currentUserId);

            if (!isMember) throw new UnauthorizedAccessException("You are not a member of this group.");

            var messages = await context.GroupMessages
                .Include(m => m.Sender)
                .Where(m => m.GroupId == groupId)
                .OrderByDescending(m => m.Timestamp)
                .Take(30)
                .ToListAsync();

            messages.Reverse();

            return new GroupChatViewModel
            {
                GroupId = groupId,
                GroupName = group.Name,
                CurrentUserId = currentUserId,
                Messages = messages.Select(m => new GroupMessageViewModel
                {
                    SenderId = m.SenderId,
                    SenderName = m.Sender.UserName,
                    Content = m.Content,
                    Timestamp = m.Timestamp,
                    ImageData = m.ImageData != null ? Convert.ToBase64String(m.ImageData) : null,
                    ImageType = m.ImageType
                }).ToList(),
                UserMessage = userMessage,
                GeminiAnswer = geminiAnswer
            };
        }

        public async Task<List<GroupMessageViewModel>> OlderGroupMessages(int groupId, int skip, int take = 30)
        {
            var currentUser = await userManager.GetUserAsync(signInManager.Context.User);

            var isMember = await context.UserGroups.AnyAsync(ug => ug.ChatGroupId == groupId && ug.UserId == currentUser.Id);

            if (!isMember) throw new UnauthorizedAccessException("You are not a member of this group.");

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
                Timestamp = m.Timestamp,
                ImageData = m.ImageData != null ? Convert.ToBase64String(m.ImageData) : null,
                ImageType = m.ImageType
            }).ToList();
        }

        public async Task<string> SaveMessage(string senderId, string receiverId, string content, bool autoTranslate)
        {
            string finalMessage = content;

            if (autoTranslate)
            {
                var translated = await chatAiService.AutoTranslateToUserLanguage(receiverId, finalMessage);
                if (!string.IsNullOrWhiteSpace(translated))
                {
                    finalMessage = translated;
                }
            }

            var censored = ""; //await chatAiService.CensorMessage(finalMessage);
            if (!string.IsNullOrEmpty(censored))
            {
                finalMessage = censored;
            }

            var msg = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = finalMessage,
                Timestamp = DateTime.UtcNow
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
                    Timestamp = DateTime.UtcNow
                };

                context.Notifications.Add(notification);
            }

            await context.SaveChangesAsync();
            return finalMessage;
        }

        public async Task<bool> SaveImage(string senderId, string receiverId, string imageData, string imageType)
        {
            if (string.IsNullOrEmpty(imageData))
            {
                return false;
            }

            byte[] imageDataBytes = Convert.FromBase64String(imageData);

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = null,
                ImageData = imageDataBytes,
                ImageType = imageType,
                Timestamp = DateTime.UtcNow,

            };

            context.Messages.Add(message);
            await context.SaveChangesAsync();

            return true;
        }

        public async Task<string> SaveGroupMessage(int groupId, string senderId, string content)
        {
            var currentUser = await userManager.GetUserAsync(signInManager.Context.User);

            var isMember = await context.UserGroups.AnyAsync(ug => ug.ChatGroupId == groupId && ug.UserId == currentUser.Id);

            if (!isMember) throw new UnauthorizedAccessException("You are not a member of this group.");

            string finalMessage = content;

            var censored = ""; //await chatAiService.CensorMessage(finalMessage);
            if (!string.IsNullOrEmpty(censored))
            {
                finalMessage = censored;
            }

            var group = await context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);

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
                        Timestamp = DateTime.UtcNow
                    };

                    context.Notifications.Add(notification);
                }
            }

            await context.SaveChangesAsync();
            return finalMessage;
        }

        public async Task<bool> SaveGroupImage(string senderId, int groupId, string imageData, string imageType)
        {
            var isMember = await context.UserGroups.AnyAsync(ug => ug.ChatGroupId == groupId && ug.UserId == senderId);

            if (!isMember) throw new UnauthorizedAccessException("You are not a member of this group.");

            if (string.IsNullOrEmpty(imageData))
            {
                return false;
            }

            byte[] imageDataBytes = Convert.FromBase64String(imageData);

            var message = new GroupMessage
            {

                GroupId = groupId,
                SenderId = senderId,
                Content = null,
                ImageData = imageDataBytes,
                ImageType = imageType,
                Timestamp = DateTime.UtcNow,

            };

            context.GroupMessages.Add(message);
            await context.SaveChangesAsync();

            return true;
        }

        public async Task ClearNotification(string userId, string friendId)
        {
            var notification = await context.Notifications
                .FirstOrDefaultAsync(n => n.SenderId == friendId && n.ReceiverId == userId && n.Type == NotificationType.Message);

            if (notification != null)
            {
                context.Notifications.Remove(notification);
                await context.SaveChangesAsync();
            }
        }

        public async Task ClearGroupNotification(string userId, int groupId)
        {
            var notification = await context.Notifications
                .FirstOrDefaultAsync(n => n.GroupId == groupId && n.ReceiverId == userId && n.Type == NotificationType.GroupMessage);

            if (notification != null)
            {
                context.Notifications.Remove(notification);
                await context.SaveChangesAsync();
            }
        }

        public async Task MarkAsReaded(string userId, string friendId)
        {
            var unreadMessages = await context.Messages
                .Where(m => m.SenderId == friendId && m.ReceiverId == userId && !m.Readed)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.Readed = true;
                }
                await context.SaveChangesAsync();
            }
        }
    }
}