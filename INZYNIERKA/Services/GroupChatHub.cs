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

        var group = await _context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupID);

        if (group == null) return;

        var groupMessage = new GroupMessage
        {
            GroupId = groupID,
            SenderId = senderID,
            Content = message,
            Timestamp = DateTime.UtcNow
        };

        _context.GroupMessages.Add(groupMessage);

        foreach (var member in group.Members)
        {
            if (member.UserId == senderID) continue;

            var existingNotification = await _context.Notifications.FirstOrDefaultAsync(n =>
                n.GroupId == groupID &&
                n.ReceiverId == member.UserId &&
                n.Type == NotificationType.GroupMessage);

            if (existingNotification == null)
            {
                var notification = new Notification
                {
                    SenderId = senderID,
                    GroupId = groupID,
                    ReceiverId = member.UserId,
                    Type = NotificationType.GroupMessage,
                    CreationDate = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
            }
        }

        await _context.SaveChangesAsync();

        await Clients.Group($"group_{groupID}").SendAsync("ReceiveGroupMessage", groupID, senderID, message);
    }
}
