using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities
{
    public class GroupMember
    {
        [Key]
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public Group Group { get; set; } = null!;

        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        public GroupRole Role { get; set; } = GroupRole.Member;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastReadTime { get; set; }
    }
}