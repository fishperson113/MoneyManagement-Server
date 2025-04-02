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
    public class TransactionRepository : ITransactionRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<TransactionRepository> _logger;

        public TransactionRepository(ApplicationDbContext context, IMapper mapper, ILogger<TransactionRepository> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<TransactionDTO> CreateTransactionAsync(CreateTransactionDTO model)
        {
            try
            {
                _logger.LogInformation("Starting transaction creation.");

                var transaction = _mapper.Map<Transaction>(model);
                transaction.TransactionID = Guid.NewGuid();

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Transaction created successfully with ID: {TransactionID}", transaction.TransactionID);
                return _mapper.Map<TransactionDTO>(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating transaction.");
                throw;
            }
        }

        public async Task<TransactionDTO> UpdateTransactionAsync(UpdateTransactionDTO model)
        {
            try
            {
                _logger.LogInformation("Updating transaction with ID: {TransactionID}", model.TransactionID);

                var transaction = await _context.Transactions.FindAsync(model.TransactionID);
                if (transaction == null)
                {
                    _logger.LogWarning("Transaction with ID {TransactionID} not found", model.TransactionID);
                    throw new KeyNotFoundException($"Transaction with ID {model.TransactionID} not found.");
                }

                _mapper.Map(model, transaction);

                _context.Transactions.Update(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Transaction with ID {TransactionID} updated successfully", transaction.TransactionID);
                return _mapper.Map<TransactionDTO>(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating transaction.");
                throw;
            }
        }

        public async Task<Guid> DeleteTransactionByIdAsync(Guid transactionId)
        {
            try
            {
                _logger.LogInformation("Deleting transaction with ID: {TransactionID}", transactionId);

                var transaction = await _context.Transactions.FindAsync(transactionId);
                if (transaction == null)
                {
                    _logger.LogWarning("Transaction with ID {TransactionID} not found", transactionId);
                    throw new KeyNotFoundException($"Transaction with ID {transactionId} not found.");
                }

                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Transaction with ID {TransactionID} deleted successfully", transactionId);
                return transactionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting transaction.");
                throw;
            }
        }

        public async Task<IEnumerable<TransactionDTO>> GetAllTransactionsAsync()
        {
            _logger.LogInformation("Fetching all transactions from the database.");

            try
            {
                var transactions = await _context.Transactions.ToListAsync();
                _logger.LogInformation("Successfully retrieved {Count} transactions.", transactions.Count);

                return _mapper.Map<IEnumerable<TransactionDTO>>(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching transactions.");
                throw;
            }
        }

        public async Task<TransactionDTO?> GetTransactionByIdAsync(Guid transactionId)
        {
            _logger.LogInformation("Fetching transaction with ID: {TransactionID}", transactionId);

            try
            {
                var transaction = await _context.Transactions.FindAsync(transactionId);

                if (transaction == null)
                {
                    _logger.LogWarning("Transaction with ID {TransactionID} not found.", transactionId);
                    return null;
                }

                _logger.LogInformation("Successfully retrieved transaction with ID: {TransactionID}", transactionId);
                return _mapper.Map<TransactionDTO>(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching transaction with ID: {TransactionID}", transactionId);
                throw;
            }
        }

        public async Task<IEnumerable<TransactionDTO>> GetTransactionsByWalletIdAsync(Guid walletId)
        {
            _logger.LogInformation("Fetching transactions for wallet with ID: {WalletID}", walletId);

            try
            {
                var transactions = await _context.Transactions
                    .Where(t => t.WalletID == walletId)
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} transactions for wallet with ID: {WalletID}",
                    transactions.Count, walletId);

                return _mapper.Map<IEnumerable<TransactionDTO>>(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching transactions for wallet with ID: {WalletID}", walletId);
                throw;
            }
        }

    }
}
