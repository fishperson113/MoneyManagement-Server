using Microsoft.AspNetCore.Identity;
using API.Models.DTOs;
using API.Helpers;
namespace API.Repositories
{
    public interface IAccountRepository
    {
        public Task<IdentityResult> SignUpAsync(SignUpDTO model);
        public Task<AuthenticationResult> SignInAsync(SignInDTO model);

        public Task<bool> ClearDatabaseAsync();
        public Task<AuthenticationResult> RefreshTokenAsync(RefreshTokenDTO model);
    }
}
