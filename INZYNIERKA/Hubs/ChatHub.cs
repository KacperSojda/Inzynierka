using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;


namespace INZYNIERKA.Hubs
{
    [Authorize]
    public class ChatHub<TUser> : Hub where TUser : User
    {
        private readonly IChatService<TUser> chatService;
        private readonly IChatAiService<TUser> chatAiService;
        private readonly PresenceTracker tracker;

        public ChatHub(IChatService<TUser> chatService, IChatAiService<TUser> chatAiService, PresenceTracker tracker)
        {
            this.chatAiService = chatAiService;
            this.chatService = chatService;
            this.tracker = tracker;
        }

        public async Task SendMessage(string senderId, string receiverId, string message, bool autoTranslate = false)
        {
            var userId = Context.UserIdentifier;
            if (senderId != userId)
            { 
                return;
            }
            if (string.IsNullOrWhiteSpace(message))
            {
                await Clients.User(senderId).SendAsync("ErrorNotification", "Message cannot be empty.");
                return;
            }

            if (message.Length > 1000)
            {
                await Clients.User(senderId).SendAsync("ErrorNotification", "Message is too long.");
                return;
            }

            try
            {
                string finalMessage = message;

                if (autoTranslate)
                {
                    var translated = await chatAiService.AutoTranslateToUserLanguage(receiverId, finalMessage);
                    if (!string.IsNullOrWhiteSpace(translated))
                    {
                        finalMessage = translated;
                    }
                }

                try
                {
                    var censored = ""; //await chatAiService.CensorMessage(finalMessage);
                    if (!string.IsNullOrEmpty(censored))
                    {
                        finalMessage = censored;
                    }
                }
                catch (Exception ex)
                {
                    
                }

                await chatService.SaveMessage(senderId, receiverId, finalMessage);
                await Clients.Users(senderId, receiverId).SendAsync("ReceiveMessage", senderId, receiverId, finalMessage);

            }
            catch (Exception ex)
            {
                await Clients.User(senderId).SendAsync("ErrorNotification", "Failed to send message.");
            }
        }

        public async Task SendImage(string senderId, string receiverId, string image, string imageType)
        {
            if (senderId != Context.UserIdentifier || string.IsNullOrEmpty(image))
            {
                return;
            }

            try
            {
                if (image.Length > 2 * 1024 * 1024)
                {
                    await Clients.Caller.SendAsync("ErrorNotification", "File is too large.");
                    return;
                }

                byte[] imageBytes = Convert.FromBase64String(image);

                var result = await chatService.SaveImage(senderId, receiverId, imageBytes, imageType);

                if (!result)
                {
                    await Clients.Caller.SendAsync("ErrorNotification", "Failed to send image.");
                    return;
                }

                await Clients.Users(senderId, receiverId).SendAsync("ReceiveImage", senderId, receiverId, image, imageType);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ErrorNotification", "Failed to send image.");
            }
        }

        public async Task<List<string>> SmartReply(string friendId)
        {
            if (Context.UserIdentifier == null)
            {
                return new List<string>();
            }

            try
            {
                var userId = Context.UserIdentifier;
                string answer = await chatAiService.ResponseHelp(userId, friendId);

                if (string.IsNullOrWhiteSpace(answer))
                {
                    return new List<string>();
                }

                var replies = answer.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                    .Take(3)
                                    .ToList();

                return replies;
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        public async Task SaveSmartReplySettings(string friendId, string tone, string custom, bool auto)
        {
            var userId = Context.UserIdentifier;
            if (userId == null) return;

            try
            {
                await chatAiService.SaveSRSettings(userId, friendId, tone, custom, auto);
            }
            catch (Exception) { }
        }

        public async Task<string> ChatSummary(string friendId, DateTime start, DateTime end)
        {
            if (Context.UserIdentifier == null)
            {
                return "Błąd autoryzacji.";
            }

            try
            {
                var userId = Context.UserIdentifier;
                string answer = await chatAiService.SummarizeChat(userId, friendId, start, end);
                return answer;
            }
            catch (Exception ex)
            {
                return "Nie udało się wygenerować podsumowania.";
            }
        }

        public async Task ClearNotifications(string userId, string friendId)
        {
            var currentUserId = Context.UserIdentifier;
            if (userId != currentUserId)
            {
                return;
            }
            try
            {
                await chatService.ClearNotification(userId, friendId);
            }
            catch (Exception ex) { }
        }
        public async Task MarkAsRead(string userId, string friendId)
        {
            var currentUserId = Context.UserIdentifier;
            if (userId != currentUserId)
            {
                return;
            }

            try
            {
                await chatService.MarkAsReaded(userId, friendId);
                await Clients.User(friendId).SendAsync("MessagesRead", userId);
            }
            catch (Exception ex) { }
        }

        public async Task WritingStatus(string senderId, string receiverId)
        {
            var userId = Context.UserIdentifier;

            if (senderId != userId)
            {
                return;
            }
            await Clients.User(receiverId).SendAsync("ReceiveTypingIndicator", senderId);
        }

        // Presence tracking methods //

        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = Context.UserIdentifier;
                if (userId == null)
                {
                    await base.OnConnectedAsync();
                    return;
                }

                var connectionId = Context.ConnectionId;

                var connected = await tracker.Connected(userId, connectionId);

                if (connected)
                {
                    await Clients.Others.SendAsync("Online", userId);
                }

            }
            catch (Exception ex) 
            { 
            
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userId = Context.UserIdentifier;
                if (userId == null)
                {
                    await base.OnDisconnectedAsync(exception);
                    return;
                }

                var connectionId = Context.ConnectionId;

                var disconnected = await tracker.Disconnected(userId, connectionId);

                if (disconnected)
                {
                    await Clients.Others.SendAsync("Offline", userId);
                }

            }
            catch (Exception ex) 
            {

            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task<bool> CheckStatus(string friendId)
        {
            try
            {
                var online = await tracker.OnlineUsers();
                return online.Any(id => id.Equals(friendId, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}

