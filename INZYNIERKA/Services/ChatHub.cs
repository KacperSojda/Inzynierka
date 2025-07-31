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

        string cenzuredMessage = await geminiService.AskAsync(message, "You are a profanity filter. Detect and censor only strong, explicit profanity such as “fuck”, “shit”, “bitch”, “cunt”, and similar words. \r\nDo not censor mild language or slang. Replace each censored word with asterisks (*) matching the exact length of the word. \r\nKeep everything else in the message unchanged. Do not add comments or explanations. Message content:");

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