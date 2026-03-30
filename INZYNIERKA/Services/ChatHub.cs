using System.Reflection;
using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public class ChatHub : Hub
{
    private readonly INZDbContext _context;
    private readonly GeminiService geminiService;
    private readonly IConfiguration configuration;

    public ChatHub(INZDbContext context, GeminiService geminiService, IConfiguration configuration)
    {
        _context = context;
        this.geminiService = geminiService;
        this.configuration = configuration;
    }

    public async Task SendMessage(string senderId, string receiverId, string message)
    {
        string CenzurePrompt = configuration["Prompts:Cenzure"];

        string cenzuredMessage = await geminiService.AskAsync(message, CenzurePrompt);

        var msg = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = cenzuredMessage,
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

        await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, receiverId, cenzuredMessage);

        await Clients.User(senderId).SendAsync("ReceiveMessage", senderId, receiverId, cenzuredMessage);
    }

    public async Task ClearNotifications(string userId, string friendId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.SenderId == friendId && n.ReceiverId == userId);

        if (notification != null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }

        await _context.SaveChangesAsync();
    }
}

