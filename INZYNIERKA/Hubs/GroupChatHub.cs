using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace INZYNIERKA.Hubs
{
    [Authorize]
    public class GroupChatHub<TUser> : Hub where TUser : User
    {
        private readonly IChatService<TUser> chatService;
        private readonly IChatAiService<TUser> chatAiService;
        private readonly ILogger<GroupChatHub<TUser>> logger;

        public GroupChatHub(IChatService<TUser> chatService, IChatAiService<TUser> chatAiService, ILogger<GroupChatHub<TUser>> logger)
        {
            this.chatService = chatService;
            this.chatAiService = chatAiService;
            this.logger = logger;
        }

        public async Task JoinGroup(string groupName)
        {
            var userId = Context.UserIdentifier;

            try
            {
                var connectionId = Context.ConnectionId;
                await Groups.AddToGroupAsync(connectionId, groupName);
                logger.LogInformation("User {UserId} joined SignalR group channel '{GroupName}' with connection {ConnectionId}.", userId, groupName, connectionId);
            }
            catch (Exception ex)
            { 
                logger.LogError(ex, "Failed to add user {UserId} to SignalR group channel '{GroupName}'.", userId, groupName);
            }
        }

        public async Task SendMessageToGroup(string groupIDString, string senderId, string message)
        {
            var userId = Context.UserIdentifier;

            if (senderId != userId)
            {
                logger.LogWarning("SendMessageToGroup blocked: SenderId {SenderId} does not match Context UserIdentifier {UserId}.", senderId, userId);
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                logger.LogWarning("SendMessageToGroup failed: User {UserId} attempted to send an empty message.", userId);
                await Clients.User(senderId).SendAsync("ErrorNotification", "Message cannot be empty.");
                return;
            }

            if (message.Length > 1000)
            {
                logger.LogWarning("SendMessageToGroup failed: User {UserId} attempted to send a message exceeding length limit.", userId);
                await Clients.User(senderId).SendAsync("ErrorNotification", "Message is too long.");
                return;
            }

            if (!int.TryParse(groupIDString, out int groupId))
            {
                logger.LogWarning("SendMessageToGroup failed: Invalid group ID '{GroupIDString}' from user {UserId}.", groupIDString, userId);
                return;
            }

            try
            {
                string finalMessage = message;
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
                    logger.LogWarning(ex, "Failed to censor group message for user {UserId} in group {GroupId}. Proceeding with original.", userId, groupId);
                }

                await chatService.SaveGroupMessage(groupId, senderId, finalMessage);
                
                string senderName = Context.User?.Identity?.Name ?? "Użytkownik";

                await Clients.Group($"group_{groupId}").SendAsync("ReceiveGroupMessage", groupId, senderId, senderName, finalMessage);
                logger.LogInformation("User {SenderId} successfully sent a message to group {GroupId}.", senderId, groupId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send message from user {SenderId} to group {GroupId}.", senderId, groupId);
                await Clients.User(senderId).SendAsync("ErrorNotification", "Failed to send message.");
            }
        }

        public async Task SendGroupImage(string groupIDString, string senderId, string image, string imageType)
        {
            var userId = Context.UserIdentifier;

            if (senderId != userId)
            {
                logger.LogWarning("SendGroupImage blocked: SenderId {SenderId} does not match Context UserIdentifier {UserId}.", senderId, userId);
                return;
            }

            if (string.IsNullOrEmpty(image))
            {
                logger.LogWarning("SendGroupImage blocked: Empty image data provided by user {UserId}.", userId);
                return;
            }

            try
            {
                if (image.Length > 2 * 1024 * 1024)
                {
                    logger.LogWarning("SendGroupImage blocked: Image data from user {UserId} exceeds size limit.", userId);
                    await Clients.Caller.SendAsync("ErrorNotification", "File is too large.");
                    return;
                }

                if (!int.TryParse(groupIDString, out int groupId))
                {
                    logger.LogWarning("SendGroupImage blocked: Invalid group ID '{GroupIDString}' from user {UserId}.", groupIDString, userId);
                    return;
                }

                byte[] imageBytes = Convert.FromBase64String(image);

                var result = await chatService.SaveGroupImage(senderId, groupId, imageBytes, imageType);

                if (!result)
                {
                    logger.LogError("Failed to save image from user {SenderId} for group {GroupId}.", senderId, groupId);
                    await Clients.Caller.SendAsync("ErrorNotification", "Failed to send image.");
                    return;
                }

                string senderName = Context.User?.Identity?.Name ?? "Użytkownik";

                await Clients.Group($"group_{groupId}").SendAsync("ReceiveGroupImage", groupId, senderId, senderName, image, imageType);
                logger.LogInformation("User {SenderId} successfully sent an image to group {GroupId}.", senderId, groupId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send image from user {SenderId} to group {GroupId}.", senderId, groupIDString);
                await Clients.User(senderId).SendAsync("ErrorNotification", "Failed to send image.");
            }
        }

        public async Task<List<string>> GroupSmartReply(string groupIDString)
        {
            var userId = Context.UserIdentifier;

            if (userId == null || !int.TryParse(groupIDString, out int groupId))
            {
                logger.LogWarning("GroupSmartReply failed: Unauthenticated user or invalid GroupId '{GroupIdString}'.", groupIDString);
                return new List<string>();
            }

            try
            {
                string answer = await chatAiService.GroupResponseHelp(userId, groupId);

                if (string.IsNullOrWhiteSpace(answer))
                {
                    logger.LogInformation("GroupSmartReply: AI returned empty response for user {UserId} in group {GroupId}.", userId, groupId);
                    return new List<string>();
                }

                var replies = answer.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                    .Take(3)
                                    .ToList();

                logger.LogInformation("GroupSmartReply generated {Count} suggestions for user {UserId} in group {GroupId}.", replies.Count, userId, groupId);
                return replies;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GroupSmartReply generation failed for user {UserId} in group {GroupIdString}.", userId, groupIDString);
                return new List<string>();
            }
        }

        public async Task<string> GroupChatSummary(string groupIDString, DateTime start, DateTime end)
        {
            var userId = Context.UserIdentifier;

            if (Context.UserIdentifier == null || !int.TryParse(groupIDString, out int groupId))
            {
                logger.LogWarning("GroupChatSummary failed: Unauthenticated user or invalid GroupId '{GroupIdString}'.", groupIDString);
                return "Błąd autoryzacji lub nieprawidłowe ID grupy.";
            }

            try
            {
                string answer = await chatAiService.SummarizeGroupChat(userId, groupId, start, end);
                logger.LogInformation("User {UserId} successfully generated chat summary for group {GroupId}.", userId, groupId);
                return answer;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GroupChatSummary generation failed for user {UserId} in group {GroupIdString}.", userId, groupIDString);
                return "Nie udało się wygenerować podsumowania. Spróbuj ponownie później.";
            }
        }

        public async Task ClearGroupNotifications(string userId, string groupIDString)
        {
            var currentUserId = Context.UserIdentifier;

            if (userId != currentUserId)
            {
                logger.LogWarning("ClearGroupNotifications blocked: target UserId {TargetId} does not match Context UserIdentifier {CurrentId}.", userId, currentUserId);
                return;
            }

            try
            {
                if (int.TryParse(groupIDString, out int groupId))
                {
                    await chatService.ClearGroupNotification(userId, groupId);
                }
                else
                {
                    logger.LogWarning("ClearGroupNotifications failed: Invalid GroupId string '{GroupIdString}' from user {UserId}.", groupIDString, userId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to clear group notifications for user {UserId} in group {GroupIdString}.", userId, groupIDString);
            }
        }

        public async Task SaveGroupSRSettings(string groupIDString, string tone, string custom, bool auto)
        {
            var userId = Context.UserIdentifier;

            if (userId == null || !int.TryParse(groupIDString, out int groupId))
            {
                logger.LogWarning("SaveGroupSRSettings failed: Unauthenticated user or invalid GroupId '{GroupIdString}'.", groupIDString);
                await Clients.Caller.SendAsync("ErrorNotification", "Authorization error");
                return;
            }

            try
            {
                await chatAiService.SaveGroupSRSettings(userId, groupId, tone, custom, auto);
                logger.LogInformation("User {UserId} saved Smart Reply settings for group {GroupId}.", userId, groupId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to save AI settings for user {UserId} in group {GroupIdString}.", userId, groupIDString);
                await Clients.Caller.SendAsync("ErrorNotification", "Failed to save AI settings.");
            }
        }
    }
}