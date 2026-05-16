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
        public DateTime? DateOfBirth { get; set; }
        [StringLength(30)]
        public string? City { get; set; }
        [StringLength(50)]
        public string? Country { get; set; }
        [StringLength(30)]
        public string? CustomStatus { get; set; }
        [Url]
        public string? SocialMediaUrl { get; set; }
        public ZodiacSign? Zodiac { get; set; }
        [StringLength(30)]
        public string? PreferredLanguages { get; set; }
        public List<string> Tags { get; set; } = new List<String>();
        public List<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
