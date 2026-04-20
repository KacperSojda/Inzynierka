using INZYNIERKA.Domain.Models;

public class GroupMembersViewModel
{
    public int GroupId { get; set; }
    public string Name { get; set; }
    public string CurrentUserId { get; set; }
    public List<GroupMember> Admins { get; set; } = new List<GroupMember>();
    public List<GroupMember> Members { get; set; } = new List<GroupMember>();
}

public class GroupMember
{
    public string UserId { get; set; }
    public string Name { get; set; }
}