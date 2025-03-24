using Microsoft.AspNetCore.Identity;
using API.Models.DTOs;
namespace API.Repositories
{
    public interface IAccountRepository
    {
        public Task<IdentityResult> SignUpAsync(SignUpDTO model);
        public Task<string> SignInAsync(SignInDTO model);

        public Task<bool> ClearDatabaseAsync();
    }
}
