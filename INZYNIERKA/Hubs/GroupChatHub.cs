using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace INZYNIERKA.Hubs
{
    [Authorize]
    public class GroupChatHub<TUser> : Hub where TUser : User
    {
        private readonly IChatService<TUser> chatService;
        private readonly IChatAiService<TUser> chatAiService;

        public GroupChatHub(IChatService<TUser> chatService, IChatAiService<TUser> chatAiService)
        {
            this.chatService = chatService;
            this.chatAiService = chatAiService;
        }

        public async Task JoinGroup(string groupName)
        {
            try
            {
                var connectionId = Context.ConnectionId;
                await Groups.AddToGroupAsync(connectionId, groupName);
            }
            catch (Exception ex) { }
        }

        public async Task SendMessageToGroup(string groupIDString, string senderId, string message)
        {
            var userId = Context.UserIdentifier;
            if (senderId != userId)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                await Clients.User(senderId).SendAsync("ErrorNotification", "Message cannot be empty.");
                return;
            }

            if (message.Length > 1000)
            {
                await Clients.User(senderId).SendAsync("ErrorNotification", "Message is too long.");
                return;
            }

            if (!int.TryParse(groupIDString, out int groupId)) return;

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
                catch (Exception ex) { }

                await chatService.SaveGroupMessage(groupId, senderId, finalMessage);
                
                string senderName = Context.User?.Identity?.Name ?? "Użytkownik";

                await Clients.Group($"group_{groupId}").SendAsync("ReceiveGroupMessage", groupId, senderId, senderName, finalMessage);
            }
            catch (Exception ex)
            {
                await Clients.User(senderId).SendAsync("ErrorNotification", "Failed to send message.");
            }
        }

        public async Task SendGroupImage(string groupIDString, string senderId, string image, string imageType)
        {
            var userId = Context.UserIdentifier;
            if (senderId != userId) return;
            
            if (string.IsNullOrEmpty(image)) return;

            try
            {
                if (image.Length > 2 * 1024 * 1024)
                {
                    await Clients.Caller.SendAsync("ErrorNotification", "File is too large.");
                    return;
                }

                if (!int.TryParse(groupIDString, out int groupId)) return;

                byte[] imageBytes = Convert.FromBase64String(image);

                var result = await chatService.SaveGroupImage(senderId, groupId, imageBytes, imageType);

                if (!result)
                {
                    await Clients.Caller.SendAsync("ErrorNotification", "Failed to send image.");
                    return;
                }

                string senderName = Context.User?.Identity?.Name ?? "Użytkownik";

                await Clients.Group($"group_{groupId}").SendAsync("ReceiveGroupImage", groupId, senderId, senderName, image, imageType);
            }
            catch (Exception ex)
            {
                await Clients.User(senderId).SendAsync("ErrorNotification", "Failed to send image.");
            }
        }

        public async Task<List<string>> GroupSmartReply(string groupIDString)
        {
            if (Context.UserIdentifier == null || !int.TryParse(groupIDString, out int groupId))
            {
                return new List<string>();
            }

            try
            {
                var userId = Context.UserIdentifier;
                
                string answer = await chatAiService.GroupResponseHelp(userId, groupId);

                if (string.IsNullOrWhiteSpace(answer))
                {
                    return new List<string>();
                }

                var replies = answer.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                    .Take(3)
                                    .ToList();

                return replies;
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        public async Task<string> GroupChatSummary(string groupIDString, DateTime start, DateTime end)
        {
            if (Context.UserIdentifier == null || !int.TryParse(groupIDString, out int groupId))
            {
                return "Błąd autoryzacji lub nieprawidłowe ID grupy.";
            }

            try
            {
                var userId = Context.UserIdentifier;
                
                string answer = await chatAiService.SummarizeGroupChat(userId, groupId, start, end);
                return answer;
            }
            catch (Exception ex)
            {
                return "Nie udało się wygenerować podsumowania. Spróbuj ponownie później.";
            }
        }

        public async Task ClearGroupNotifications(string userId, string groupIDString)
        {
            var currentUserId = Context.UserIdentifier;
            if (userId != currentUserId)
            {
                return;
            }

            try
            {
                if (int.TryParse(groupIDString, out int groupId))
                {
                    await chatService.ClearGroupNotification(userId, groupId);
                }
            }
            catch (Exception ex) { }
        }

        public async Task SaveGroupSRSettings(string groupIDString, string tone, string custom, bool auto)
        {
            var userId = Context.UserIdentifier;
            if (userId == null || !int.TryParse(groupIDString, out int groupId))
            {
                await Clients.Caller.SendAsync("ErrorNotification", "Authorization error");
                return;
            }

            try
            {
                await chatAiService.SaveGroupSRSettings(userId, groupId, tone, custom, auto);
            }
            catch (Exception)
            {
                await Clients.Caller.SendAsync("ErrorNotification", "Failed to save AI settings.");
            }
        }
    }
}