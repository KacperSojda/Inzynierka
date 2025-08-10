using System.Text.RegularExpressions;
using INZYNIERKA.Data;
using INZYNIERKA.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class GroupChatHub : Hub
{
    private readonly INZDbContext _context;
    private readonly UserManager<User> _userManager;
    public GroupChatHub(INZDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    public async Task JoinGroup(string groupName)
    {
       await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task SendMessageToGroup(string groupIDString, string senderID, string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        if (!int.TryParse(groupIDString, out int groupID))
        {
            return;
        }

        var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupID);
        if (!groupExists)
        {
            return;
        }

        var groupMessage = new GroupMessage
        {
            GroupId = groupID,
            SenderId = senderID,
            Content = message,
            Timestamp = DateTime.UtcNow
        };

        _context.GroupMessages.Add(groupMessage);
        await _context.SaveChangesAsync();

        await Clients.Group($"group_{groupID}").SendAsync("ReceiveGroupMessage", groupID, senderID, message);
    }
}
