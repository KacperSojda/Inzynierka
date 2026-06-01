namespace INZYNIERKA.Hubs
{
    public class PresenceTracker
    {
        private static readonly Dictionary<string, List<string>> onlineUsers = new Dictionary<string, List<string>>();

        public Task<bool> Connected(string userId, string connectionId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Task.FromResult(false);
            }

            bool online = false;

            lock (onlineUsers)
            {
                if (onlineUsers.ContainsKey(userId))
                {
                    onlineUsers[userId].Add(connectionId);
                }
                else
                {
                    onlineUsers.Add(userId, new List<string> { connectionId });
                    online = true; 
                }
            }
            return Task.FromResult(online);
        }

        public Task<bool> Disconnected(string userId, string connectionId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Task.FromResult(false);
            }

            bool offline = false;

            lock (onlineUsers)
            {
                if (!onlineUsers.ContainsKey(userId))
                {
                    return Task.FromResult(offline);
                }

                onlineUsers[userId].Remove(connectionId);

                if (onlineUsers[userId].Count == 0)
                {
                    onlineUsers.Remove(userId);
                    offline = true;
                }
            }
            return Task.FromResult(offline);
        }

        public Task<string[]> OnlineUsers()
        {
            string[] onlineUsersTab;

            lock (onlineUsers)
            {
                onlineUsersTab = onlineUsers.OrderBy(k => k.Key).Select(k => k.Key).ToArray();
            }
            return Task.FromResult(onlineUsersTab);
        }
    }
}