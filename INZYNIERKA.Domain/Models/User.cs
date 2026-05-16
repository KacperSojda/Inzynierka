using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INZYNIERKA.Domain.Models
{
    public enum ZodiacSign
    {
        Aries,          // Baran
        Taurus,         // Byk
        Gemini,         // Bliźnięta
        Cancer,         // Rak
        Leo,            // Lew
        Virgo,          // Panna
        Libra,          // Waga
        Scorpio,        // Skorpion
        Sagittarius,    // Strzelec
        Capricorn,      // Koziorożec
        Aquarius,       // Wodnik
        Pisces          // Ryby
    }
    public class User : IdentityUser
    {
        public string PublicDescription { get; set; }
        public string PrivateDescription { get; set; }
        public string Avatar { get; set; }
        public DateTime? DateOfBirth { get; set; }
        [MaxLength(30)]
        public string? City { get; set; }
        [MaxLength(50)]
        public string? Country { get; set; }
        [MaxLength(30)]
        public string? CustomStatus { get; set; }
        public string? CoverPhoto { get; set; }
        public string? SocialMediaUrl { get; set; }
        public ZodiacSign? Zodiac { get; set; }
        [MaxLength(30)]
        public string? PreferredLanguages { get; set; }
        public DateTime LastActiveDate { get; set; } = DateTime.UtcNow;
        [NotMapped]
        public int? Age
        {
            get
            {
                if (!DateOfBirth.HasValue) return null;
                var today = DateTime.Today;
                var age = today.Year - DateOfBirth.Value.Year;
                if (DateOfBirth.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
        }
        public List<UserTag> UserTags { get; set; }
        public List<Notification> SendedNotifications { get; set; }
        public List<Notification> ReceivedNotifications { get; set; }
        public List<UserFriend> SendedFriendRequests { get; set; }
        public List<UserFriend> ReceivedFriendRequests { get; set; }
        public List<Message> SendedMessages { get; set; }
        public List<Message> ReceivedMessages { get; set; }
        public List<UserGroup> JoinedGroups { get; set; }
    }
}
