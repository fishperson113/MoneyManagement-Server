using API.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Repositories
{
    public interface ITransactionRepository
    {
        Task<IEnumerable<TransactionDTO>> GetAllTransactionsAsync();
        Task<TransactionDTO?> GetTransactionByIdAsync(Guid transactionId);
        Task<TransactionDTO> CreateTransactionAsync(CreateTransactionDTO model);
        Task<TransactionDTO> UpdateTransactionAsync(UpdateTransactionDTO model);
        Task<Guid> DeleteTransactionByIdAsync(Guid transactionId);
    }
}
