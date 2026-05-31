namespace INZYNIERKA.Services.ViewModels
{
    public class GroupChatViewModel
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public string CurrentUserId { get; set; }
        public List<GroupMessageViewModel> Messages { get; set; }
        public string UserMessage { get; set; }
        public string GeminiAnswer { get; set; }
    }

    public class GroupMessageViewModel
    {
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public string? ImageData { get; set; } = null;
        public string? ImageType { get; set; }
        public bool IsImage => !string.IsNullOrEmpty(ImageData);
    }
}
