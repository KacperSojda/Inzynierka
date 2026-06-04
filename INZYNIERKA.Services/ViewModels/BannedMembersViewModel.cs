namespace INZYNIERKA.Services.ViewModels
{
    public class BannedMembersViewModel
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public List<BannedUserDto> BannedUsers { get; set; } = new();
    }

    public class BannedUserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}