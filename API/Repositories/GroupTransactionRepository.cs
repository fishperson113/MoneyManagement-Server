using API.Data;
using API.Models.DTOs;
using API.Models.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Repositories
{
    public class GroupTransactionRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<GroupTransactionRepository> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GroupTransactionRepository(ApplicationDbContext context, IMapper mapper, ILogger<GroupTransactionRepository> logger, IHttpContextAccessor httpContextAccessor)
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

        public async Task<GroupTransactionDTO> CreateGroupTransactionAsync(CreateGroupTransactionDTO dto)
        {
            try
            {
                _logger.LogInformation("Creating group transaction for GroupFundID: {GroupFundID}", dto.GroupFundID);

                var groupFund = await _context.GroupFunds
                    .Include(gf => gf.Group)
                        .ThenInclude(g => g.Members)
                    .FirstOrDefaultAsync(gf => gf.GroupFundID == dto.GroupFundID);

                if (groupFund == null)
                    throw new Exception("GroupFund not found");

                var userId = GetCurrentUserId();
                if (!groupFund.Group.Members.Any(m => m.UserId == userId))
                    throw new UnauthorizedAccessException("You are not a member of this group");

                var entity = _mapper.Map<GroupTransaction>(dto);
                entity.GroupTransactionID = Guid.NewGuid();

                _context.GroupTransactions.Add(entity);

                // Cập nhật tổng quỹ
                if (dto.Type.ToLower() == "income")
                    groupFund.TotalFundsIn += dto.Amount;
                else
                    groupFund.TotalFundsOut += dto.Amount;

                groupFund.Balance = groupFund.TotalFundsIn - groupFund.TotalFundsOut;
                groupFund.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return _mapper.Map<GroupTransactionDTO>(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group transaction");
                throw;
            }
        }

        public async Task<IEnumerable<GroupTransactionDTO>> GetGroupTransactionsByFundIdAsync(Guid groupFundId)
        {
            try
            {
                var userId = GetCurrentUserId();

                var groupFund = await _context.GroupFunds
                    .Include(gf => gf.Group)
                        .ThenInclude(g => g.Members)
                    .FirstOrDefaultAsync(gf => gf.GroupFundID == groupFundId);

                if (groupFund == null)
                    throw new Exception("GroupFund not found");

                if (!groupFund.Group.Members.Any(m => m.UserId == userId))
                    throw new UnauthorizedAccessException("You are not a member of this group");

                var transactions = await _context.GroupTransactions
                    .Include(gt => gt.UserWallet)
                    .Include(gt => gt.UserCategory)
                    .Where(gt => gt.GroupFundID == groupFundId)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<GroupTransactionDTO>>(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching group transactions");
                throw;
            }
        }

        public async Task<GroupTransactionDTO> UpdateGroupTransactionAsync(UpdateGroupTransactionDTO dto)
        {
            try
            {
                var userId = GetCurrentUserId();

                var transaction = await _context.GroupTransactions
                    .Include(gt => gt.GroupFund)
                        .ThenInclude(gf => gf.Group)
                            .ThenInclude(g => g.Members)
                    .FirstOrDefaultAsync(gt => gt.GroupTransactionID == dto.GroupTransactionID);

                if (transaction == null)
                    throw new Exception("Group transaction not found");

                if (!transaction.GroupFund.Group.Members.Any(m => m.UserId == userId))
                    throw new UnauthorizedAccessException("You are not a member of this group");

                // Optional: Rollback old transaction effect
                if (transaction.Type == "income")
                    transaction.GroupFund.TotalFundsIn -= transaction.Amount;
                else
                    transaction.GroupFund.TotalFundsOut -= transaction.Amount;

                // Update transaction
                _mapper.Map(dto, transaction);

                // Re-apply new amount
                if (transaction.Type == "income")
                    transaction.GroupFund.TotalFundsIn += transaction.Amount;
                else
                    transaction.GroupFund.TotalFundsOut += transaction.Amount;

                transaction.GroupFund.Balance = transaction.GroupFund.TotalFundsIn - transaction.GroupFund.TotalFundsOut;
                transaction.GroupFund.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return _mapper.Map<GroupTransactionDTO>(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group transaction");
                throw;
            }
        }

        public async Task<bool> DeleteGroupTransactionAsync(Guid transactionId)
        {
            try
            {
                var userId = GetCurrentUserId();

                var transaction = await _context.GroupTransactions
                    .Include(gt => gt.GroupFund)
                        .ThenInclude(gf => gf.Group)
                            .ThenInclude(g => g.Members)
                    .FirstOrDefaultAsync(gt => gt.GroupTransactionID == transactionId);

                if (transaction == null)
                    throw new Exception("Group transaction not found");

                if (!transaction.GroupFund.Group.Members.Any(m => m.UserId == userId))
                    throw new UnauthorizedAccessException("You are not a member of this group");

                // Adjust group fund before delete
                if (transaction.Type == "income")
                    transaction.GroupFund.TotalFundsIn -= transaction.Amount;
                else
                    transaction.GroupFund.TotalFundsOut -= transaction.Amount;

                transaction.GroupFund.Balance = transaction.GroupFund.TotalFundsIn - transaction.GroupFund.TotalFundsOut;
                transaction.GroupFund.UpdatedAt = DateTime.UtcNow;

                _context.GroupTransactions.Remove(transaction);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting group transaction");
                throw;
            }
        }
    }
}
