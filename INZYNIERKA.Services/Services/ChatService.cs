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
        private readonly INZDbContext<TUser> _context;
        private readonly UserManager<TUser> _userManager;
        private readonly SignInManager<TUser> _signInManager;
        private readonly IChatAiService<TUser> _chatAiService;

        public ChatService(INZDbContext<TUser> context, UserManager<TUser> userManager, SignInManager<TUser> signInManager, IChatAiService<TUser> chatAiService)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _chatAiService = chatAiService;
        }

        /// <summary>Retrieves the chat history and view model for a private conversation.</summary>
        /// <param name="currentUserId">The ID of the current user.</param>
        /// <param name="friendId">The ID of the friend.</param>
        /// <param name="userMessage">The current text in the input field.</param>
        /// <param name="geminiAnswer">The generated AI response to display.</param>
        /// <returns>ChatViewModel containing messages and AI configuration.</returns>
        public async Task<ChatViewModel> Chat(string currentUserId, string friendId, string userMessage, string geminiAnswer)
        {
            var user = await _userManager.FindByIdAsync(currentUserId);
            var friend = await _userManager.FindByIdAsync(friendId);

            if (user == null || friend == null) return null;

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == friendId) ||
                            (m.SenderId == friendId && m.ReceiverId == currentUserId))
                .OrderByDescending(m => m.Timestamp)
                .Take(30)
                .ToListAsync();

            messages.Reverse();

            var relation = await _context.UserFriends
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
                    Read = m.Read
                }).ToList(),
                UserMessage = userMessage,
                GeminiAnswer = geminiAnswer,
                GeminiQuestion = "",
                Tone = relation?.Tone ?? "casual",
                Custom = relation?.Custom,
                Auto = relation?.SmartReplies ?? true
            };
        }

        /// <summary>Retrieves an older batch of messages for a private chat.</summary>
        /// <param name="userId">The ID of the current user.</param>
        /// <param name="friendId">The ID of the friend.</param>
        /// <param name="skip">The number of messages to skip.</param>
        /// <param name="take">The number of messages to retrieve.</param>
        /// <returns>A list of older messages.</returns>
        public async Task<List<MessageViewModel>> OlderMessages(string userId, string friendId, int skip, int take = 30)
        {
            var messages = await _context.Messages
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

        /// <summary>Retrieves the chat history and view model for a group conversation.</summary>
        /// <param name="currentUserId">The ID of the current user.</param>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="userMessage">The current text in the input field.</param>
        /// <param name="geminiAnswer">The generated AI response to display.</param>
        /// <returns> GroupChatViewModel containing messages and AI configuration.</returns>
        public async Task<GroupChatViewModel> GroupChat(string currentUserId, int groupId, string userMessage, string geminiAnswer)
        {
            var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return null;

            var member = await _context.UserGroups.FirstOrDefaultAsync(ug => ug.ChatGroupId == groupId && ug.UserId == currentUserId);

            if (member == null) throw new UnauthorizedAccessException("You are not a member of this group.");

            var messages = await _context.GroupMessages
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
                GeminiAnswer = geminiAnswer,
                Tone = member.Tone ?? "casual",
                Custom = member.Custom,
                Auto = member.SmartReplies
            };
        }

        /// <summary>Retrieves an older batch of messages for a group chat.</summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="skip">The number of messages to skip.</param>
        /// <param name="take">The number of messages to retrieve.</param>
        /// <returns>A list of older group messages.</returns>
        public async Task<List<GroupMessageViewModel>> OlderGroupMessages(int groupId, int skip, int take = 30)
        {
            var currentUser = await _userManager.GetUserAsync(_signInManager.Context.User);

            var isMember = await _context.UserGroups.AnyAsync(ug => ug.ChatGroupId == groupId && ug.UserId == currentUser.Id);

            if (!isMember) throw new UnauthorizedAccessException("You are not a member of this group.");

            var messages = await _context.GroupMessages
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

        /// <summary>Saves a new message in a private chat and creates a notification for the receiver.</summary>
        /// <param name="senderId">The ID of the message sender.</param>
        /// <param name="receiverId">The ID of the message receiver.</param>
        /// <param name="content">The text of the message.</param>
        /// <param name="autoTranslate">Indicates whether the message should be automatically translated to the receiver's language.</param>
        /// <returns>The final message.</returns>
        public async Task<string> SaveMessage(string senderId, string receiverId, string content, bool autoTranslate)
        {
            string finalMessage = content;

            if (autoTranslate)
            {
                var translated = await _chatAiService.AutoTranslateToUserLanguage(receiverId, finalMessage);
                if (!string.IsNullOrWhiteSpace(translated))
                {
                    finalMessage = translated;
                }
            }

            var censored = await _chatAiService.CensorMessage(finalMessage);
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

            _context.Messages.Add(msg);

            var existingNotification = await _context.Notifications
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

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
            return finalMessage;
        }

        /// <summary>Saves a base64-encoded image as a message in a private chat.</summary>
        /// <param name="senderId">The ID of the message sender.</param>
        /// <param name="receiverId">The ID of the message receiver.</param>
        /// <param name="imageData">The base64-encoded image data.</param>
        /// <param name="imageType">The type of the image.</param>
        /// <returns>True if the image was saved successfully, otherwise false.</returns>
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

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>Saves a new message in a group chat and creates notifications for other group members.</summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="senderId">The ID of the message sender.</param>
        /// <param name="content">The text content of the message.</param>
        /// <returns>The final message content.</returns>
        public async Task<string> SaveGroupMessage(int groupId, string senderId, string content)
        {
            var currentUser = await _userManager.GetUserAsync(_signInManager.Context.User);

            var isMember = await _context.UserGroups.AnyAsync(ug => ug.ChatGroupId == groupId && ug.UserId == currentUser.Id);

            if (!isMember) throw new UnauthorizedAccessException("You are not a member of this group.");

            string finalMessage = content;

            var censored = await _chatAiService.CensorMessage(finalMessage);
            if (!string.IsNullOrEmpty(censored))
            {
                finalMessage = censored;
            }

            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            var groupMessage = new GroupMessage
            {
                GroupId = groupId,
                SenderId = senderId,
                Content = content,
                Timestamp = DateTime.UtcNow
            };

            _context.GroupMessages.Add(groupMessage);

            foreach (var member in group.Members)
            {
                if (member.UserId == senderId) continue;

                var existingNotification = await _context.Notifications.FirstOrDefaultAsync(n =>
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

                    _context.Notifications.Add(notification);
                }
            }

            await _context.SaveChangesAsync();
            return finalMessage;
        }

        /// <summary>Saves a base64-encoded image as a message in a group chat.</summary>
        /// <param name="senderId">The ID of the message sender.</param>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="imageData">The base64-encoded image data.</param>
        /// <param name="imageType">The type of the image.</param>
        /// <returns>True if the image was saved successfully, otherwise false.</returns>
        public async Task<bool> SaveGroupImage(string senderId, int groupId, string imageData, string imageType)
        {
            var isMember = await _context.UserGroups.AnyAsync(ug => ug.ChatGroupId == groupId && ug.UserId == senderId);

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

            _context.GroupMessages.Add(message);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>Clears message notification from a specific friend.</summary>
        /// <param name="userId">The ID of the receiving user.</param>
        /// <param name="friendId">The ID of the sender.</param>
        public async Task ClearNotification(string userId, string friendId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.SenderId == friendId && n.ReceiverId == userId && n.Type == NotificationType.Message);

            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>Clears message notification for a specific group chat.</summary>
        /// <param name="userId">The ID of the receiving user.</param>
        /// <param name="groupId">The ID of the group.</param>
        public async Task ClearGroupNotification(string userId, int groupId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.GroupId == groupId && n.ReceiverId == userId && n.Type == NotificationType.GroupMessage);

            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>Marks all unread messages from a friend as read.</summary>
        /// <param name="userId">The ID of the receiving user.</param>
        /// <param name="friendId">The ID of the sender.</param>
        public async Task MarkAsReaded(string userId, string friendId)
        {
            var unreadMessages = await _context.Messages
                .Where(m => m.SenderId == friendId && m.ReceiverId == userId && !m.Read)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.Read = true;
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}