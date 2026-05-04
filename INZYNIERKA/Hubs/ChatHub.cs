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

            string censoredMessage = message; //await chatAiService.CensorMessageAsync(safemessage);

            await chatService.SavePrivateMessageAsync(senderId, receiverId, censoredMessage);

            await Clients.Users(senderId, receiverId).SendAsync("ReceiveMessage", senderId, receiverId, censoredMessage);
        }

        public async Task SendImage(string senderId, string receiverId, string base64Image, string imageType)
        {
            if (string.IsNullOrEmpty(base64Image)) return;

            if(base64Image.Length > 2 * 1024 * 1024)
            {
                await Clients.Caller.SendAsync("ErrorNotification", "Obrazek jest za duży.");
                return;
            }

            byte[] imageBytes = Convert.FromBase64String(base64Image);

            var success = await chatService.SaveImageMessageAsync(senderId, receiverId, imageBytes, imageType);

            if (!success)
            {
                await Clients.Caller.SendAsync("ErrorNotification", "Nie udało się wysłać obrazka.");
                return;
            }

            await Clients.Users(senderId, receiverId).SendAsync("ReceiveImage", senderId, receiverId, base64Image, imageType);
        }

        public async Task ClearNotifications(string userId, string friendId)
        {
            await chatService.ClearMessageNotificationAsync(userId, friendId);
        }
        public async Task MarkAsRead(string userId, string friendId)
        {
            await chatService.MarkMessagesAsReadAsync(userId, friendId);

            await Clients.User(friendId).SendAsync("MessagesRead", userId);
        }

        public async Task SendTypingIndicator(string senderId, string receiverId)
        {
            await Clients.User(receiverId).SendAsync("ReceiveTypingIndicator", senderId);
        }

        // Presence tracking methods //

        public override async Task OnConnectedAsync()
        {
            if(Context.UserIdentifier == null)
            {
                await base.OnConnectedAsync();
                return;
            }

            var isOnline = await tracker.UserConnected(Context.UserIdentifier, Context.ConnectionId);

            if (isOnline)
            {
                await Clients.Others.SendAsync("UserIsOnline", Context.UserIdentifier);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.UserIdentifier == null)
            {
                await base.OnDisconnectedAsync(exception);
                return;
            }

            var isOffline = await tracker.UserDisconnected(Context.UserIdentifier, Context.ConnectionId);



            if (isOffline)
            {
                await Clients.Others.SendAsync("UserIsOffline", Context.UserIdentifier);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task<bool> CheckIfUserIsOnline(string friendId)
        {
            var onlineUsers = await tracker.GetOnlineUsers();
            return onlineUsers.Any(id => id.Equals(friendId, StringComparison.OrdinalIgnoreCase));
        }
    }
}

