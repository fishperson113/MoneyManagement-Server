using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace API.Models.DTOs
{
    public class SignInDTO
    {
        [Required, EmailAddress]
        [SwaggerSchema("User's email address")]
        [SwaggerParameter("mon@example.com")]
        public string Email { get; set; } = null!;

        [Required]
        [SwaggerSchema("User's password")]
        [SwaggerParameter("Mon@123")]
        public string Password { get; set; } = null!;
    }
}
