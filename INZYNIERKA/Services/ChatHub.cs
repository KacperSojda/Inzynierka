using System.Reflection;
using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class ChatHub : Hub
{
    private readonly INZDbContext _context;
    private readonly GeminiService geminiService;

    public ChatHub(INZDbContext context, GeminiService geminiService)
    {
        _context = context;
        this.geminiService = geminiService;
    }

    public async Task SendMessage(string senderId, string receiverId, string message)
    {

        var msg = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = await geminiService.AskAsync(message, "Ocenzuruj podną wiadomosc za pomoca znakow '*' "),
            DateTime = DateTime.UtcNow
        };

        _context.Messages.Add(msg);

        var existingNotification = await _context.Notifications
        .FirstOrDefaultAsync(n =>
            n.SenderId == senderId &&
            n.ReceiverId == receiverId &&
            n.Type == NotificationType.Message);

        if (existingNotification == null)
        {
            var notification = new Notification
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Type = NotificationType.Message,
                CreationDate = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync();

        await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, receiverId, message);

        await Clients.User(senderId).SendAsync("ReceiveMessage", senderId, receiverId, message);
    }
}