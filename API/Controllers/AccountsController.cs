using API.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using API.Helpers;
using API.Models.Entities;
using AutoMapper;
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
    }
}
