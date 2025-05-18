// API/Models/Entities/GroupMessage.cs
using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities
{
    public class GroupMessage
    {
        [Key]
        public Guid MessageId { get; set; }
        public Guid GroupId { get; set; }
        public Group Group { get; set; } = null!;

        public string SenderId { get; set; } = null!;
        public ApplicationUser Sender { get; set; } = null!;

        public string Content { get; set; } = null!;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
