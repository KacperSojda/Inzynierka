using System.ComponentModel.DataAnnotations;

namespace INZYNIERKA.Domain.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string SenderId { get; set; }
        public User Sender { get; set; }
        public string ReceiverId { get; set; }
        public User Receiver { get; set; }

        [MaxLength(1000)]
        public string? Content { get; set; }
        public byte[]? ImageData { get; set; }
        public string? ImageType { get; set; }
        public DateTime DateTime { get; set; }
    }
}
