using INZYNIERKA.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace INZYNIERKA.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService chatService;
        private readonly IChatAiService chatAiService;
        private readonly PresenceTracker tracker;

        public ChatHub(IChatService chatService, IChatAiService chatAiService, PresenceTracker tracker)
        {
            this.chatAiService = chatAiService;
            this.chatService = chatService;
            this.tracker = tracker;
        }

        public async Task SendMessage(string senderId, string receiverId, string message)
        {
            if (senderId != Context.UserIdentifier){ return; }
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
                try
                {
                    var censored = ""; //await chatAiService.CensorMessageAsync(finalMessage);
                    if (!string.IsNullOrEmpty(censored)) finalMessage = censored;
                }
                catch (Exception ex) { }

                await chatService.SavePrivateMessageAsync(senderId, receiverId, finalMessage);
                await Clients.Users(senderId, receiverId).SendAsync("ReceiveMessage", senderId, receiverId, finalMessage);

            }
            catch (Exception ex)
            {
                await Clients.User(senderId).SendAsync("ErrorNotification", "Failed to send message.");
            }
        }

        public async Task SendImage(string senderId, string receiverId, string base64Image, string imageType)
        {
            if (senderId != Context.UserIdentifier) return;
            if (string.IsNullOrEmpty(base64Image)) return;

            try
            {
                if (base64Image.Length > 2 * 1024 * 1024)
                {
                    await Clients.Caller.SendAsync("ErrorNotification", "File is too large.");
                    return;
                }

                byte[] imageBytes = Convert.FromBase64String(base64Image);

                var success = await chatService.SaveImageMessageAsync(senderId, receiverId, imageBytes, imageType);

                if (!success)
                {
                    await Clients.Caller.SendAsync("ErrorNotification", "Failed to send image.");
                    return;
                }

                await Clients.Users(senderId, receiverId).SendAsync("ReceiveImage", senderId, receiverId, base64Image, imageType);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ErrorNotification", "Failed to send image.");
            }
        }

        public async Task ClearNotifications(string userId, string friendId)
        {
            if (userId != Context.UserIdentifier) return;

            try
            {
                await chatService.ClearMessageNotificationAsync(userId, friendId);
            }
            catch (Exception ex) { }
        }
        public async Task MarkAsRead(string userId, string friendId)
        {
            if (userId != Context.UserIdentifier) return;

            try
            {
                await chatService.MarkMessagesAsReadAsync(userId, friendId);
                await Clients.User(friendId).SendAsync("MessagesRead", userId);
            }
            catch (Exception ex) { }
        }

        public async Task SendTypingIndicator(string senderId, string receiverId)
        {
            if(senderId != Context.UserIdentifier) return;
            await Clients.User(receiverId).SendAsync("ReceiveTypingIndicator", senderId);
        }

        // Presence tracking methods //

        public override async Task OnConnectedAsync()
        {
            try
            {
                if (Context.UserIdentifier != null)
                {
                    var isOnline = await tracker.UserConnected(Context.UserIdentifier, Context.ConnectionId);
                    if (isOnline)
                    {
                        await Clients.Others.SendAsync("UserIsOnline", Context.UserIdentifier);
                    }
                }
            }
            catch (Exception ex) { }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                if (Context.UserIdentifier != null)
                {
                    var isOffline = await tracker.UserDisconnected(Context.UserIdentifier, Context.ConnectionId);
                    if (isOffline)
                    {
                        await Clients.Others.SendAsync("UserIsOffline", Context.UserIdentifier);
                    }
                }
            }
            catch (Exception ex) { }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task<bool> CheckIfUserIsOnline(string friendId)
        {
            try
            {
                var onlineUsers = await tracker.GetOnlineUsers();
                return onlineUsers.Any(id => id.Equals(friendId, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}

