using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities;

public class PostCommentReply
{
    [Key]
    public Guid ReplyId { get; set; }
    public string Content { get; set; } = null!;
    public Guid CommentId { get; set; }
    public PostComment Comment { get; set; } = null!;
    public string AuthorId { get; set; } = null!;
    public ApplicationUser Author { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public Guid? ParentReplyId { get; set; }
    public PostCommentReply? ParentReply { get; set; }
    public ICollection<PostCommentReply> Replies { get; set; } = new List<PostCommentReply>();
}