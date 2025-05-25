using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities
{
    public class Group
    {
        [Key]
        public Guid GroupId { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatorId { get; set; } = null!;
        public ApplicationUser Creator { get; set; } = null!;

        public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
        public ICollection<GroupMessage> Messages { get; set; } = new List<GroupMessage>();
        public ICollection<GroupFund> Funds { get; set; } = new List<GroupFund>();

    }
}
