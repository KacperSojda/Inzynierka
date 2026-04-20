using INZYNIERKA.Data;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

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

        public async Task SendMessageToGroup(string groupIDString, string senderID, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            if (!int.TryParse(groupIDString, out int groupID)) return;

            await chatService.SaveGroupMessageAsync(groupID, senderID, message);

            await Clients.Group($"group_{groupID}").SendAsync("ReceiveGroupMessage", groupID, senderID, message);
        }
    }
}
