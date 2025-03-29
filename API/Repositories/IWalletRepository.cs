using API.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Repositories
{
    public interface IWalletRepository
    {
        Task<IEnumerable<WalletDTO>> GetAllWalletsAsync();
        Task<WalletDTO?> GetWalletByIdAsync(Guid walletId);
        Task<WalletDTO> CreateWalletAsync(CreateWalletDTO model);
        Task<WalletDTO> UpdateWalletAsync(UpdateWalletDTO model);
        Task<Guid> DeleteWalletByIdAsync(Guid walletId);
    }
}
