using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities
{
    public class Message
    {
        public Guid MessageID { get; set; }
        public required string SenderId { get; set; }
        public required string ReceiverId { get; set; }
        public required string Content { get; set; }
        public DateTime SentAt { get; set; }

        public required ApplicationUser Sender { get; set; }
        public required ApplicationUser Receiver { get; set; }
    }
}
