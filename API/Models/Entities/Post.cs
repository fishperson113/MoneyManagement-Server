using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities
{
    public class Post
    {
        [Key]
        public Guid PostId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string AuthorId { get; set; } = null!;
        public ApplicationUser Author { get; set; } = null!;
        public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
        public ICollection<PostComment> Comments { get; set; } = new List<PostComment>();

        public string? MediaUrl { get; set; }
        public string? MediaType { get; set; }
        
        // New post targeting fields
        public PostTargetType TargetType { get; set; } = PostTargetType.Friends;
        public string? TargetGroupIds { get; set; } // JSON array of group IDs for Groups target type
    }
}
