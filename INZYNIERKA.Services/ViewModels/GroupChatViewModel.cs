namespace INZYNIERKA.Services.ViewModels
{
    public class GroupChatViewModel
    {
        public int groupID { get; set; }
        public string groupName { get; set; }
        public string currentUserID { get; set; }
        public List<GroupMessageViewModel> messages { get; set; }
        public string UserMessage { get; set; }
        public string GeminiAnswer { get; set; }
    }

    public class GroupMessageViewModel
    {
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public DateTime DateTime { get; set; }
        public string? ImageDataBase64 { get; set; }
        public string? ImageType { get; set; }
        public bool IsImage => !string.IsNullOrEmpty(ImageDataBase64);
    }
}
