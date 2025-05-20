using Microsoft.AspNetCore.Identity;

namespace INZYNIERKA.Models
{
    public class User : IdentityUser
    {
        public string PublicDescription { get; set; }
        public string PrivateDescription { get; set; }
        public string Avatar { get; set; }
        public List<UserTag> UserTags { get; set; }
    }
}
