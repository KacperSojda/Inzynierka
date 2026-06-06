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
        private readonly IChatService<TUser> _chatService;
        private readonly IChatAiService<TUser> _chatAiService;
        private readonly ILogger<GroupChatHub<TUser>> _logger;

        public GroupChatHub(IChatService<TUser> chatService, IChatAiService<TUser> chatAiService, ILogger<GroupChatHub<TUser>> logger)
        {
            _chatService = chatService;
            _chatAiService = chatAiService;
            _logger = logger;
        }

        public async Task JoinGroup(string groupName)
        {
            var userId = Context.UserIdentifier;

            try
            {
                var connectionId = Context.ConnectionId;
                await Groups.AddToGroupAsync(connectionId, groupName);
                _logger.LogInformation("User {UserId} joined SignalR group channel '{GroupName}' with connection {ConnectionId}.", userId, groupName, connectionId);
            }
            catch (Exception ex)
            { 
                _logger.LogError(ex, "Failed to add user {UserId} to SignalR group channel '{GroupName}'.", userId, groupName);
            }
        }

        public async Task SendMessageToGroup(string groupIdString, string senderId, string message)
        {
            var userId = Context.UserIdentifier;

            if (senderId != userId)
            {
                _logger.LogWarning("SendMessageToGroup blocked: SenderId {SenderId} does not match Context UserIdentifier {UserId}.", senderId, userId);
                return;
            }

            if (string.IsNullOrWhiteSpace(message) || message.Length > 1000)
            {
                _logger.LogWarning("SendMessageToGroup failed: User {UserId} attempted to send an empty or too long message.", userId);
                await Clients.User(senderId).SendAsync("ErrorNotification", "Message cannot be empty or too long.");
                return;
            }

            if (!int.TryParse(groupIdString, out int groupId))
            {
                _logger.LogWarning("SendMessageToGroup failed: Invalid group ID '{GroupIdString}' from user {UserId}.", groupIdString, userId);
                return;
            }

            try
            {
                string finalMessage = await _chatService.SaveGroupMessage(groupId, senderId, message);
                string senderName = Context.User?.Identity?.Name ?? "Użytkownik";

                await Clients.Group($"group_{groupId}").SendAsync("ReceiveGroupMessage", groupId, senderId, senderName, finalMessage);
                _logger.LogInformation("User {SenderId} successfully sent a message to group {GroupId}.", senderId, groupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message from user {SenderId} to group {GroupId}.", senderId, groupId);
                await Clients.User(senderId).SendAsync("ErrorNotification", "Failed to send message.");
            }
        }

        public async Task SendGroupImage(string groupIdString, string senderId, string image, string imageType)
        {
            var userId = Context.UserIdentifier;

            if (senderId != userId)
            {
                _logger.LogWarning("SendGroupImage blocked: SenderId {SenderId} does not match Context UserIdentifier {UserId}.", senderId, userId);
                return;
            }

            if (string.IsNullOrEmpty(image) || image.Length > 2 * 1024 * 1024)
            {
                _logger.LogWarning("SendGroupImage blocked: Empty or too large image data provided by user {UserId}.", userId);
                await Clients.User(senderId).SendAsync("ErrorNotification", "Message cannot be empty or too long.");
                return;
            }

            try
            {
                if (!int.TryParse(groupIdString, out int groupId))
                {
                    _logger.LogWarning("SendGroupImage blocked: Invalid group ID '{GroupIdString}' from user {UserId}.", groupIdString, userId);
                    return;
                }

                var result = await _chatService.SaveGroupImage(senderId, groupId, image, imageType);

                if (!result)
                {
                    _logger.LogError("Failed to save image from user {SenderId} for group {GroupId}.", senderId, groupId);
                    await Clients.Caller.SendAsync("ErrorNotification", "Failed to send image.");
                    return;
                }

                string senderName = Context.User?.Identity?.Name ?? "User";

                await Clients.Group($"group_{groupId}").SendAsync("ReceiveGroupImage", groupId, senderId, senderName, image, imageType);
                _logger.LogInformation("User {SenderId} successfully sent an image to group {GroupId}.", senderId, groupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send image from user {SenderId} to group {GroupId}.", senderId, groupIdString);
                await Clients.User(senderId).SendAsync("ErrorNotification", "Failed to send image.");
            }
        }

        public async Task<List<string>> GroupSmartReply(string groupIdString)
        {
            var userId = Context.UserIdentifier;

            if (userId == null || !int.TryParse(groupIdString, out int groupId))
            {
                _logger.LogWarning("GroupSmartReply failed: Unauthenticated user or invalid GroupId '{GroupIdString}'.", groupIdString);
                return new List<string>();
            }

            try
            {
                return await _chatAiService.GroupResponseHelp(userId, groupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GroupSmartReply generation failed for user {UserId} in group {GroupIdString}.", userId, groupIdString);
                return new List<string>();
            }
        }

        public async Task<string> GroupChatSummary(string groupIdString, DateTime start, DateTime end)
        {
            var userId = Context.UserIdentifier;

            if (Context.UserIdentifier == null || !int.TryParse(groupIdString, out int groupId))
            {
                _logger.LogWarning("GroupChatSummary failed: Unauthenticated user or invalid GroupId '{GroupIdString}'.", groupIdString);
                return "Error: Unauthenticated user or invalid GroupId.";
            }

            try
            {
                string answer = await _chatAiService.SummarizeGroupChat(userId, groupId, start, end);
                _logger.LogInformation("User {UserId} successfully generated chat summary for group {GroupId}.", userId, groupId);
                return answer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GroupChatSummary generation failed for user {UserId} in group {GroupIdString}.", userId, groupIdString);
                return "Failed to generate chat summary.";
            }
        }

        public async Task ClearGroupNotifications(string userId, string groupIdString)
        {
            var currentUserId = Context.UserIdentifier;

            if (userId != currentUserId)
            {
                _logger.LogWarning("ClearGroupNotifications blocked: target UserId {TargetId} does not match Context UserIdentifier {CurrentId}.", userId, currentUserId);
                return;
            }

            try
            {
                if (int.TryParse(groupIdString, out int groupId))
                {
                    await _chatService.ClearGroupNotification(userId, groupId);
                }
                else
                {
                    _logger.LogWarning("ClearGroupNotifications failed: Invalid GroupId string '{GroupIdString}' from user {UserId}.", groupIdString, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear group notifications for user {UserId} in group {GroupIdString}.", userId, groupIdString);
            }
        }

        public async Task SaveGroupSRSettings(string groupIdString, string tone, string custom, bool auto)
        {
            var userId = Context.UserIdentifier;

            if (userId == null || !int.TryParse(groupIdString, out int groupId))
            {
                _logger.LogWarning("SaveGroupSRSettings failed: Unauthenticated user or invalid GroupId '{GroupIdString}'.", groupIdString);
                await Clients.Caller.SendAsync("ErrorNotification", "Authorization error");
                return;
            }

            try
            {
                await _chatAiService.SaveGroupSRSettings(userId, groupId, tone, custom, auto);
                _logger.LogInformation("User {UserId} saved Smart Reply settings for group {GroupId}.", userId, groupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save AI settings for user {UserId} in group {GroupIdString}.", userId, groupIdString);
                await Clients.Caller.SendAsync("ErrorNotification", "Failed to save AI settings.");
            }
        }
    }
}