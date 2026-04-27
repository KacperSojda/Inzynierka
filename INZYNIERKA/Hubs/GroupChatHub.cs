using INZYNIERKA.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Web;

namespace INZYNIERKA.Hubs
{
    public class GroupChatHub : Hub
    {
        private readonly IChatService chatService;
        public GroupChatHub(IChatService chatService)
        {
            this.chatService = chatService;
        }

        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task SendMessageToGroup(string groupIDString, string senderId, string message)
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

            if (!int.TryParse(groupIDString, out int groupID)) return;

            await chatService.SaveGroupMessageAsync(groupID, senderId, message);

            await Clients.Group($"group_{groupID}").SendAsync("ReceiveGroupMessage", groupID, senderId, message);
        }

        public async Task SendGroupImage(string groupIDString, string senderId, string base64Image, string imageType)
        {
            if (string.IsNullOrEmpty(base64Image)) return;

            if (base64Image.Length > 2 * 1024 * 1024)
            {
                await Clients.Caller.SendAsync("ErrorNotification", "Obrazek jest za duży.");
                return;
            }

            byte[] imageBytes = Convert.FromBase64String(base64Image);

            if (!int.TryParse(groupIDString, out int groupID)) return;

            var success = await chatService.SaveGroupImageMessageAsync(senderId, groupID, imageBytes, imageType);

            if (!success)
            {
                await Clients.Caller.SendAsync("ErrorNotification", "Nie udało się wysłać obrazka.");
                return;
            }

            await Clients.Group($"group_{groupIDString}").SendAsync("ReceiveGroupImage", groupID, senderId, base64Image, imageType);
        }

        public async Task ClearNotifications(string userId, string friendId)
        {
            await chatService.ClearMessageNotificationAsync(userId, friendId);
        }
    }
}
