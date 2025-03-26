using System.ComponentModel.DataAnnotations;

namespace API.Models.DTOs
{
    public class RefreshTokenDTO
    {
        public required string Token { get; set; }
        public required string RefreshToken { get; set; }
    }
}
