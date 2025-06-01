using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities
{
    public class PostLike
    {
        [Key]
        public Guid LikeId { get; set; }
        public Guid PostId { get; set; }
        public Post Post { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}