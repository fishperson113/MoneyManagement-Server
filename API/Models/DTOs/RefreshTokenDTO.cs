using System.ComponentModel.DataAnnotations;

namespace API.Models.DTOs
{
    public class RefreshTokenDTO
    {
        public required string ExpiredToken { get; set; }
    }
}
