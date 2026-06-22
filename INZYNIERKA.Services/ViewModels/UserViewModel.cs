using INZYNIERKA.Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace INZYNIERKA.Services.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; } = "";
        public string? Avatar { get; set; }
        public string? UserName { get; set; }
        public string? PublicDescription { get; set; }
        public string? PrivateDescription { get; set; }
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }
        [StringLength(30)]
        public string? City { get; set; }
        [StringLength(50)]
        public string? Country { get; set; }
        [StringLength(30)]
        public string? Status { get; set; }
        public string? Cover { get; set; }
        [Url]
        public string? SocialMedia { get; set; }
        public ZodiacSign? Zodiac { get; set; }
        [StringLength(30)]
        public string? Language { get; set; }
        public int? Age { get; set; }
        public bool IsFriend { get; set; }
        public List<string> Tags { get; set; } = new List<String>();
        public List<Notification> Notifications { get; set; } = new List<Notification>();
                
    }
}
