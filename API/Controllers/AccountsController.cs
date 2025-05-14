using API.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using API.Helpers;
using API.Models.Entities;
using AutoMapper;
using API.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountRepository accountRepository;

        public AccountsController(IAccountRepository repo)
        {
            accountRepository = repo;
        }
        [HttpPost("SignUp")]
        public async Task<IActionResult> SignUpAsync(SignUpDTO model)
        {
            var result = await accountRepository.SignUpAsync(model);
            if (result.Succeeded)
            {
                return Ok(result.Succeeded);                          
            }
            return BadRequest();
        }
        [HttpPost("SignIn")]
        public async Task<IActionResult> SignInAsync(SignInDTO model)
        {
            var token = await accountRepository.SignInAsync(model);
            if (String.IsNullOrEmpty(token.Token))
            {
                return Unauthorized();
            }
            return Ok(token.Token);
        }
        [HttpDelete("ClearDatabase")]
        [Authorize(Roles = AppRole.Admin)]
        public async Task<IActionResult> ClearDatabaseAsync()
        {
            var result = await accountRepository.ClearDatabaseAsync();
            if (result)
            {
                return Ok("Database cleared successfully.");
            }
            return StatusCode(500, "Failed to clear database.");
        }

        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshTokenAsync(RefreshTokenDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Success = false, Message = "Invalid model state", Errors = ModelState });
                }

                var authResult = await accountRepository.RefreshTokenAsync(model);

                if (string.IsNullOrEmpty(authResult.Token))
                {
                    return Unauthorized(new { Success = false, Message = "Token refresh failed", Errors = authResult.Errors });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    Token = authResult.Token
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex });
            }
        }

        [HttpGet("users")]
        [Authorize(Roles = AppRole.Admin)] // hoặc bỏ Authorize nếu bạn muốn public
        public async Task<IActionResult> GetAllUsers([FromServices] ApplicationDbContext dbContext)
        {
            var users = await dbContext.Users
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.FirstName,
                    u.LastName
                })
                .ToListAsync();

            return Ok(users);
        }
        [HttpPost("avatar")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            // Verify file is an image
            if (!file.ContentType.StartsWith("image/"))
            {
                return BadRequest("Only image files are allowed");
            }

            // Get current user ID from claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var result = await accountRepository.UploadAvatarAsync(userId, file);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error uploading avatar: {ex.Message}");
            }
        }
        [HttpPost("upload")]
        public IActionResult UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }
            // Process the file (save it, etc.)
            // ...
            return Ok(new { FileName = file.FileName, Length = file.Length });
        }
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var profile = await accountRepository.GetUserProfileAsync(User);
                return Ok(profile);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
        [HttpGet("users/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetUserById(string userId)
        {
            try
            {
                var userProfile = await accountRepository.GetUserByIdAsync(userId);
                return Ok(userProfile);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving user profile: {ex.Message}");
            }
        }
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateUserDTO model)
        {
            try
            {
                // Get current user ID from claims
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Update user profile
                var updatedProfile = await accountRepository.UpdateUserAsync(userId, model);
                return Ok(updatedProfile);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating profile: {ex.Message}");
            }
        }

    }
}
