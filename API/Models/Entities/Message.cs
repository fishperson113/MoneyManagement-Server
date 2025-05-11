using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities
{
    public class Message
    {
        public Guid MessageID { get; set; }
        public string SenderId { get; set; } = null!;
        public string ReceiverId { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime SentAt { get; set; }

        public ApplicationUser Sender { get; set; } = null!;
        public ApplicationUser Receiver { get; set; } = null!;
    }
}
