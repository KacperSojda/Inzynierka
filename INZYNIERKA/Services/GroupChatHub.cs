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
        Console.WriteLine("co jest4");
        if (string.IsNullOrWhiteSpace(message)) return;

        Console.WriteLine("co jest3");

        if (!int.TryParse(groupIDString, out int groupID))
        {
            return;
        }

        Console.WriteLine("co jest2");

        var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupID);
        if (!groupExists)
        {
            return;
        }

        Console.WriteLine("co jest3");

        var groupMessage = new GroupMessage
        {
            GroupId = groupID,
            SenderId = senderID,
            Content = message,
            Timestamp = DateTime.UtcNow
        };

        _context.GroupMessages.Add(groupMessage);

        Console.WriteLine("co jest3");

        var group = await _context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupID);

        if (group == null) return;

        foreach (var member in group.Members)
        {
            if (member.UserId == senderID) continue;

            var existingNotification = await _context.Notifications.FirstOrDefaultAsync(n =>
                n.GroupId == groupIDString &&
                n.ReceiverId == member.UserId &&
                n.Type == NotificationType.Message);

            if (existingNotification == null)
            {
                var notification = new Notification
                {
                    SenderId = senderID,
                    GroupId = groupIDString,
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
