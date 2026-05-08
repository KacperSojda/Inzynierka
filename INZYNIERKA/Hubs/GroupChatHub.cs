using INZYNIERKA.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Web;

namespace INZYNIERKA.Hubs
{
    [Authorize]
    public class GroupChatHub : Hub
    {
        private readonly IChatService chatService;
        private readonly IChatAiService chatAiService;
        public GroupChatHub(IChatService chatService, IChatAiService chatAiService)
        {
            this.chatService = chatService;
            this.chatAiService = chatAiService;
        }

        public async Task JoinGroup(string groupName)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            }
            catch (Exception ex) { }
        }

        public async Task SendMessageToGroup(string groupIDString, string senderId, string message)
        {
            if (senderId != Context.UserIdentifier) return;

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

            if (!int.TryParse(groupIDString, out int groupID)) return;

            try
            {
                string finalMessage = message;
                try
                {
                    var censored = ""; //await chatAiService.CensorMessageAsync(finalMessage);
                    if (!string.IsNullOrEmpty(censored)) finalMessage = censored;

                }
                catch (Exception ex) { }

                await chatService.SaveGroupMessageAsync(groupID, senderId, message);
                await Clients.Group($"group_{groupID}").SendAsync("ReceiveGroupMessage", groupID, senderId, message);
            }
            catch (Exception ex)
            {
                await Clients.User(senderId).SendAsync("ErrorNotification", "Failed to send message.");
            }
        }

        public async Task SendGroupImage(string groupIDString, string senderId, string base64Image, string imageType)
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

                if (!int.TryParse(groupIDString, out int groupID)) return;

                var success = await chatService.SaveGroupImageMessageAsync(senderId, groupID, imageBytes, imageType);

                if (!success)
                {
                    await Clients.Caller.SendAsync("ErrorNotification", "Failed to send image.");
                    return;
                }

                await Clients.Group($"group_{groupID}").SendAsync("ReceiveGroupImage", groupID, senderId, base64Image, imageType);
            }
            catch (Exception ex)
            {
                await Clients.User(senderId).SendAsync("ErrorNotification", "Failed to send image.");
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
    }
}
