using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

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
            string cenzuredMessage = await chatAiService.CensorMessageAsync(message);

            await chatService.SavePrivateMessageAsync(senderId, receiverId, cenzuredMessage);

            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, receiverId, cenzuredMessage);
            await Clients.User(senderId).SendAsync("ReceiveMessage", senderId, receiverId, cenzuredMessage);
        }

        public async Task ClearNotifications(string userId, string friendId)
        {
            await chatService.ClearMessageNotificationAsync(userId, friendId);
        }
    }
}

