using API.Data;
using API.Models.DTOs;
using API.Models.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Repositories
{
    public class WalletRepository : IWalletRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<WalletRepository> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public WalletRepository(ApplicationDbContext context, IMapper mapper, ILogger<WalletRepository> logger,
        IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }
        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User is not authenticated");
        }
        public async Task<WalletDTO> CreateWalletAsync(CreateWalletDTO model)
        {
            try
            {
                _logger.LogInformation("Starting wallet creation.");

                var wallet = _mapper.Map<Wallet>(model);
                wallet.WalletID = Guid.NewGuid();
                wallet.UserId = GetCurrentUserId();

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

                var userId = GetCurrentUserId();
                var wallet = await _context.Wallets
                    .FirstOrDefaultAsync(w => w.WalletID == model.WalletID && w.UserId == userId);

                if (wallet == null)
                {
                    _logger.LogWarning("Wallet with ID {WalletID} not found or doesn't belong to the current user", model.WalletID);
                    throw new KeyNotFoundException($"Wallet with ID {model.WalletID} not found or access denied.");
                }

                // Store the original userId to preserve it
                var originalUserId = wallet.UserId;

                _mapper.Map(model, wallet);

                // Don't allow changing the owner
                wallet.UserId = originalUserId;

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

                var userId = GetCurrentUserId();
                var wallet = await _context.Wallets
                    .FirstOrDefaultAsync(w => w.WalletID == walletId && w.UserId == userId);

                if (wallet == null)
                {
                    _logger.LogWarning("Wallet with ID {WalletID} not found or doesn't belong to the current user", walletId);
                    throw new KeyNotFoundException($"Wallet with ID {walletId} not found or access denied.");
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
            _logger.LogInformation("Fetching wallets for the current user.");

            try
            {
                var userId = GetCurrentUserId();
                var wallets = await _context.Wallets
                    .Where(w => w.UserId == userId)
                    .Include(w => w.Transactions)
                    .ToListAsync();
                var walletDtos = _mapper.Map<IEnumerable<WalletDTO>>(wallets).ToList();

                _logger.LogInformation("Successfully retrieved {Count} wallets for user {UserId}.", wallets.Count, userId);

                foreach (var wallet in wallets)
                {
                    var walletDto = walletDtos.First(dto => dto.WalletID == wallet.WalletID);

                    if (wallet.Transactions != null && wallet.Transactions.Any())
                    {
                        decimal transactionsSum = wallet.Transactions.Sum(t =>
                            t.Type.Equals("Income", StringComparison.OrdinalIgnoreCase) ? t.Amount : -t.Amount);

                        walletDto.Balance = wallet.Balance + transactionsSum;
                    }
                }
                return walletDtos;
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
                var userId = GetCurrentUserId();
                var wallet = await _context.Wallets
                    .Include(w => w.Transactions)
                    .FirstOrDefaultAsync(w => w.WalletID == walletId && w.UserId == userId);

                if (wallet == null)
                {
                    _logger.LogWarning("Wallet with ID {WalletID} not found or doesn't belong to the current user.", walletId);
                    return null;
                }
                var walletDto = _mapper.Map<WalletDTO>(wallet);

                _logger.LogInformation("Successfully retrieved wallet with ID: {WalletID}", walletId);

                if (wallet.Transactions != null && wallet.Transactions.Any())
                {
                    decimal transactionsSum = wallet.Transactions.Sum(t =>
                        t.Type.Equals("Income", StringComparison.OrdinalIgnoreCase) ? t.Amount : -t.Amount);

                    walletDto.Balance = wallet.Balance + transactionsSum;
                }
                return walletDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching wallet with ID: {WalletID}", walletId);
                throw;
            }
        }
    }
}
