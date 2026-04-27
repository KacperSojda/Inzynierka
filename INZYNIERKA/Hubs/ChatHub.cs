using INZYNIERKA.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Web;

namespace INZYNIERKA.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService chatService;
        private readonly IChatAiService chatAiService;

        public ChatHub(IChatService chatService, IChatAiService chatAiService)
        {
            this.chatAiService = chatAiService;
            this.chatService = chatService;
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

            string safemessage = HttpUtility.HtmlEncode(message);

            string censoredMessage = await chatAiService.CensorMessageAsync(safemessage);

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
    }
}

