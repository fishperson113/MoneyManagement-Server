using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models.Entities
{
    public class GroupTransactionComment
    {
        [Key]
        public Guid CommentId { get; set; } = Guid.NewGuid();

        public required Guid GroupTransactionId { get; set; }
        public required GroupTransaction GroupTransaction { get; set; }

        public required string UserId { get; set; }
        public required ApplicationUser User { get; set; }

        [Required]
        public required string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}