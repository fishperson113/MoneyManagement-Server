using Microsoft.AspNetCore.Identity;

namespace API.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public ICollection<Wallet>? Wallets { get; set; }
        public ICollection<Category>? Categories { get; set; }
        public ICollection<RefreshToken>? RefreshTokens { get; set; }

        public ICollection<Message> MessagesSent { get; set; } = new List<Message>();
        public ICollection<Message> MessagesReceived { get; set; } = new List<Message>();

    }
}
