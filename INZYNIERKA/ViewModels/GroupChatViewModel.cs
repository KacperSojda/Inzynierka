using INZYNIERKA.Models;

namespace INZYNIERKA.ViewModels
{
    public class GroupChatViewModel
    {
        public int groupID { get; set; }
        public string currentUserID { get; set; }
        public List<GroupMessage> messages { get; set; }
    }
}
