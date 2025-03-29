using API.Data;
using API.Models.DTOs;
using API.Models.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Repositories
{
    public class WalletRepository : IWalletRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<WalletRepository> _logger;

        public WalletRepository(ApplicationDbContext context, IMapper mapper, ILogger<WalletRepository> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<WalletDTO> CreateWalletAsync(CreateWalletDTO model)
        {
            try
            {
                _logger.LogInformation("Starting wallet creation.");

                var wallet = _mapper.Map<Wallet>(model);
                wallet.WalletID = Guid.NewGuid();

                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Wallet created successfully with ID: {WalletID}", wallet.WalletID);
                return _mapper.Map<WalletDTO>(wallet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating wallet.");
                throw;
            }
        }

        public async Task<WalletDTO> UpdateWalletAsync(UpdateWalletDTO model)
        {
            try
            {
                _logger.LogInformation("Updating wallet with ID: {WalletID}", model.WalletID);

                var wallet = await _context.Wallets.FindAsync(model.WalletID);
                if (wallet == null)
                {
                    _logger.LogWarning("Wallet with ID {WalletID} not found", model.WalletID);
                    throw new KeyNotFoundException($"Wallet with ID {model.WalletID} not found.");
                }

                _mapper.Map(model, wallet);

                _context.Wallets.Update(wallet);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Wallet with ID {WalletID} updated successfully", wallet.WalletID);
                return _mapper.Map<WalletDTO>(wallet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating wallet.");
                throw;
            }
        }

        public async Task<Guid> DeleteWalletByIdAsync(Guid walletId)
        {
            try
            {
                _logger.LogInformation("Deleting wallet with ID: {WalletID}", walletId);

                var wallet = await _context.Wallets.FindAsync(walletId);
                if (wallet == null)
                {
                    _logger.LogWarning("Wallet with ID {WalletID} not found", walletId);
                    throw new KeyNotFoundException($"Wallet with ID {walletId} not found.");
                }

                _context.Wallets.Remove(wallet);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Wallet with ID {WalletID} deleted successfully", walletId);
                return walletId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting wallet.");
                throw;
            }
        }

        public async Task<IEnumerable<WalletDTO>> GetAllWalletsAsync()
        {
            _logger.LogInformation("Fetching all wallets from the database.");

            try
            {
                var wallets = await _context.Wallets.ToListAsync();
                _logger.LogInformation("Successfully retrieved {Count} wallets.", wallets.Count);

                return _mapper.Map<IEnumerable<WalletDTO>>(wallets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching wallets.");
                throw;
            }
        }

        public async Task<WalletDTO?> GetWalletByIdAsync(Guid walletId)
        {
            _logger.LogInformation("Fetching wallet with ID: {WalletID}", walletId);

            try
            {
                var wallet = await _context.Wallets.FindAsync(walletId);

                if (wallet == null)
                {
                    _logger.LogWarning("Wallet with ID {WalletID} not found.", walletId);
                    return null;
                }

                _logger.LogInformation("Successfully retrieved wallet with ID: {WalletID}", walletId);
                return _mapper.Map<WalletDTO>(wallet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching wallet with ID: {WalletID}", walletId);
                throw;
            }
        }
    }
}
