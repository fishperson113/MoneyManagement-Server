using Microsoft.AspNetCore.Identity;
using API.Models.DTOs;
using API.Helpers;
using API.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace API.Repositories
{
    public interface IAccountRepository
    {
        public Task<IdentityResult> SignUpAsync(SignUpDTO model);
        public Task<AuthenticationResult> SignInAsync(SignInDTO model);

        public Task<bool> ClearDatabaseAsync();
        public Task<AuthenticationResult> RefreshTokenAsync(RefreshTokenDTO model);

        public Task<AvatarDTO> UploadAvatarAsync(string userId, IFormFile file);
        public Task<UserProfileDTO> GetUserProfileAsync(ClaimsPrincipal user);
        public Task<UserProfileDTO> GetUserByIdAsync(string userId);
        public Task<UserProfileDTO> UpdateUserAsync(string userId, UpdateUserDTO model, IFormFile? avatarFile = null);

    }
}
