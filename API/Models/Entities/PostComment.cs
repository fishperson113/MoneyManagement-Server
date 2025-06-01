using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities
{
    public class PostComment
    {
        [Key]
        public Guid CommentId { get; set; }
        public string Content { get; set; } = null!;
        public Guid PostId { get; set; }
        public Post Post { get; set; } = null!;
        public string AuthorId { get; set; } = null!;
        public ApplicationUser Author { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}