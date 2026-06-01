using System.ComponentModel.DataAnnotations;

namespace INZYNIERKA.Domain.Models
{
    public class GroupMessage
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public Group ChatGroup { get; set; }

        public string SenderId { get; set; }
        public User Sender { get; set; }

        [MaxLength(1000)]
        public string? Content { get; set; }
        public byte[]? ImageData { get; set; }
        public string? ImageType { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
