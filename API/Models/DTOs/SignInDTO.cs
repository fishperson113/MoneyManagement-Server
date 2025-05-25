using Microsoft.OpenApi.Any;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace API.Models.DTOs
{
    public class SignInDTO
    {
        [Required, EmailAddress]
        [SwaggerSchema(Description = "User's email address", Format = "email")]

        public string Email { get; set; } = null!;

        [Required]
        [SwaggerSchema(Description = "User's password")]
        public string Password { get; set; } = null!;
    }
}
