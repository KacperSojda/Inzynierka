using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;


namespace INZYNIERKA.Hubs
{
    [Authorize]
    public class ChatHub<TUser> : Hub where TUser : User
    {
        private readonly IChatService<TUser> chatService;
        private readonly IChatAiService<TUser> chatAiService;
        private readonly PresenceTracker tracker;
        private readonly ILogger<ChatHub<TUser>> logger;

        public ChatHub(IChatService<TUser> chatService, IChatAiService<TUser> chatAiService, PresenceTracker tracker, ILogger<ChatHub<TUser>> logger)
        {
            this.chatAiService = chatAiService;
            this.chatService = chatService;
            this.tracker = tracker;
            this.logger = logger;
        }

        public async Task SendMessage(string senderId, string receiverId, string message, bool autoTranslate = false)
        {
            var userId = Context.UserIdentifier;
            if (senderId != userId)
            {
                logger.LogWarning("SendMessage blocked: SenderId {SenderId} does not match Context UserIdentifier {UserId}.", senderId, userId);
                return;
            }
            if (string.IsNullOrWhiteSpace(message))
            {
                logger.LogWarning("SendMessage failed: User {UserId} attempted to send an empty message.", userId);
                await Clients.User(senderId).SendAsync("ErrorNotification", "Message cannot be empty.");
                return;
            }

            if (message.Length > 1000)
            {
                logger.LogWarning("SendMessage failed: User {UserId} attempted to send a message exceeding length limit.", userId);
                await Clients.User(senderId).SendAsync("ErrorNotification", "Message is too long.");
                return;
            }

            try
            {
                string finalMessage = message;

                if (autoTranslate)
                {
                    var translated = await chatAiService.AutoTranslateToUserLanguage(receiverId, finalMessage);
                    if (!string.IsNullOrWhiteSpace(translated))
                    {
                        finalMessage = translated;
                    }
                }

                try
                {
                    var censored = ""; //await chatAiService.CensorMessage(finalMessage);
                    if (!string.IsNullOrEmpty(censored))
                    {
                        finalMessage = censored;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Censorship failed for message from {SenderId} to {ReceiverId}.", senderId, receiverId);
                }

                await chatService.SaveMessage(senderId, receiverId, finalMessage);
                await Clients.Users(senderId, receiverId).SendAsync("ReceiveMessage", senderId, receiverId, finalMessage);

                logger.LogInformation("User {SenderId} successfully sent a message to {ReceiverId}.", senderId, receiverId);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SendMessage failed for user {SenderId} to {ReceiverId}.", senderId, receiverId);
                await Clients.User(senderId).SendAsync("ErrorNotification", "Failed to send message.");
            }
        }

        public async Task SendImage(string senderId, string receiverId, string image, string imageType)
        {
            if (senderId != Context.UserIdentifier || string.IsNullOrEmpty(image))
            {
                logger.LogWarning("SendImage blocked: Invalid authorization or empty image data for user {UserId}.", Context.UserIdentifier);
                return;
            }

            try
            {
                if (image.Length > 2 * 1024 * 1024)
                {
                    logger.LogWarning("SendImage failed: Image too large from user {UserId}.", senderId);
                    await Clients.Caller.SendAsync("ErrorNotification", "File is too large.");
                    return;
                }

                byte[] imageBytes = Convert.FromBase64String(image);

                var result = await chatService.SaveImage(senderId, receiverId, imageBytes, imageType);

                if (!result)
                {
                    logger.LogError("SendImage failed: Error saving image from user {UserId} to {ReceiverId}.", senderId, receiverId);
                    await Clients.Caller.SendAsync("ErrorNotification", "Failed to send image.");
                    return;
                }

                await Clients.Users(senderId, receiverId).SendAsync("ReceiveImage", senderId, receiverId, image, imageType);
                logger.LogInformation("User {SenderId} successfully sent an image to {ReceiverId}.", senderId, receiverId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SendImage failed for user {SenderId} to {ReceiverId}.", senderId, receiverId);
                await Clients.Caller.SendAsync("ErrorNotification", "Failed to send image.");
            }
        }

        public async Task<List<string>> SmartReply(string friendId)
        {
            if (Context.UserIdentifier == null)
            {
                return new List<string>();
            }

            try
            {
                var userId = Context.UserIdentifier;
                string answer = await chatAiService.ResponseHelp(userId, friendId);

                if (string.IsNullOrWhiteSpace(answer))
                {
                    logger.LogInformation("SmartReply: AI returned empty response for user {UserId}.", userId);
                    return new List<string>();
                }

                var replies = answer.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                    .Take(3)
                                    .ToList();

                logger.LogInformation("SmartReply generated {Count} suggestions for user {UserId}.", replies.Count, userId);
                return replies;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SmartReply failed for user {UserId}.", Context.UserIdentifier);
                return new List<string>();
            }
        }

        public async Task SaveSmartReplySettings(string friendId, string tone, string custom, bool auto)
        {
            var userId = Context.UserIdentifier;
            if (userId == null)
            {
                await Clients.Caller.SendAsync("ErrorNotification", "Authorization error");
                return;
            }

            try
            {
                await chatAiService.SaveSRSettings(userId, friendId, tone, custom, auto);
                logger.LogInformation("User {UserId} saved Smart Reply settings for chat with {FriendId}.", userId, friendId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to save Smart Reply settings for user {UserId} and friend {FriendId}.", userId, friendId);
                await Clients.Caller.SendAsync("ErrorNotification", "Failed to save settings.");
            }
        }

        public async Task<string> ChatSummary(string friendId, DateTime start, DateTime end)
        {
            if (Context.UserIdentifier == null)
            {
                return "Błąd autoryzacji.";
            }

            try
            {
                var userId = Context.UserIdentifier;
                string answer = await chatAiService.SummarizeChat(userId, friendId, start, end);
                logger.LogInformation("User {UserId} successfully generated chat summary with {FriendId}.", userId, friendId);
                return answer;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate chat summary for user {UserId} with friend {FriendId}.", Context.UserIdentifier, friendId);
                return "Nie udało się wygenerować podsumowania.";
            }
        }

        public async Task ClearNotifications(string userId, string friendId)
        {
            var currentUserId = Context.UserIdentifier;
            if (userId != currentUserId)
            {
                return;
            }
            try
            {
                await chatService.ClearNotification(userId, friendId);
            }
            catch (Exception ex)
            { 
                logger.LogError(ex, "Failed to clear notifications for user {UserId} and friend {FriendId}.", userId, friendId);
            }
        }
        public async Task MarkAsRead(string userId, string friendId)
        {
            var currentUserId = Context.UserIdentifier;
            if (userId != currentUserId)
            {
                return;
            }

            try
            {
                await chatService.MarkAsReaded(userId, friendId);
                await Clients.User(friendId).SendAsync("MessagesRead", userId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to mark messages as read for user {UserId} and friend {FriendId}.", userId, friendId);
            }
        }

        public async Task WritingStatus(string senderId, string receiverId)
        {
            var userId = Context.UserIdentifier;

            if (senderId != userId)
            {
                return;
            }

            try
            {
                await Clients.User(receiverId).SendAsync("ReceiveTypingIndicator", senderId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send typing indicator from {SenderId} to {ReceiverId}.", senderId, receiverId);
            }
        }

        // Presence tracking methods //

        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = Context.UserIdentifier;
                if (userId == null)
                {
                    logger.LogWarning("OnConnectedAsync: Unauthenticated client attempted to connect.");
                    await base.OnConnectedAsync();
                    return;
                }

                var connectionId = Context.ConnectionId;

                var connected = await tracker.Connected(userId, connectionId);

                if (connected)
                {
                    logger.LogInformation("User {UserId} connected to SignalR (ConnectionId: {ConnectionId}).", userId, connectionId);
                    await Clients.Others.SendAsync("Online", userId);
                }

            }
            catch (Exception ex) 
            { 
                logger.LogError(ex, "OnConnectedAsync failed for user {UserId}.", Context.UserIdentifier);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userId = Context.UserIdentifier;
                if (userId == null)
                {
                    await base.OnDisconnectedAsync(exception);
                    return;
                }

                var connectionId = Context.ConnectionId;

                var disconnected = await tracker.Disconnected(userId, connectionId);

                if (disconnected)
                {
                    logger.LogInformation("User {UserId} disconnected from SignalR (ConnectionId: {ConnectionId}).", userId, connectionId);
                    await Clients.Others.SendAsync("Offline", userId);
                }

            }
            catch (Exception ex) 
            {
                logger.LogError(ex, "OnDisconnectedAsync failed for user {UserId}.", Context.UserIdentifier);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task<bool> CheckStatus(string friendId)
        {
            try
            {
                var online = await tracker.OnlineUsers();
                return online.Any(id => id.Equals(friendId, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CheckStatus failed for friendId {FriendId}.", friendId);
                return false;
            }
        }
    }
}

