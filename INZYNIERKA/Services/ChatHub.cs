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

        string cenzuredMessage = await geminiService.AskAsync(message, "You are a profanity filter. Detect offensive words (e.g., profanity, hate, sexual content, racism) and replace each such word with asterisks (*), with the number of asterisks exactly matching the number of characters in the word. \r\nDo not change any other part of the message. Do not comment. Return only the censored message. Message content:");

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
}