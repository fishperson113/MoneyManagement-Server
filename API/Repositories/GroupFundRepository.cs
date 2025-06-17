using API.Data;
using API.Models.DTOs;
using API.Models.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace API.Repositories
{
    public class GroupFundRepository : IGroupFundRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<GroupFundRepository> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GroupFundRepository(ApplicationDbContext context, IMapper mapper, ILogger<GroupFundRepository> logger, IHttpContextAccessor httpContextAccessor)
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

        public async Task<GroupFundDTO> CreateGroupFundAsync(CreateGroupFundDTO dto)
        {
            try
            {
                _logger.LogInformation("Starting group fund creation for Name: {GroupID}", dto.GroupID);

                var entity = _mapper.Map<GroupFund>(dto);
                entity.GroupFundID = Guid.NewGuid();

                _context.GroupFunds.Add(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Group fund created successfully with ID: {GroupFundID}", entity.GroupFundID);

                return _mapper.Map<GroupFundDTO>(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating group fund: {GroupID}", dto.GroupID);
                throw;
            }
        }

        public async Task<IEnumerable<GroupFundDTO>> GetGroupFundsByGroupIdAsync(GetGroupFundByGroupIdDTO model)
        {
            _logger.LogInformation("Fetching group fund with ID: {GroupID}", model.GroupID);

            try
            {
                var userId = GetCurrentUserId();

                var group = await _context.Groups
                    .Include(g => g.Members)
                    .FirstOrDefaultAsync(g => g.GroupId == model.GroupID);

                if (group == null)
                    throw new Exception("Group not found");

                if (!group.Members.Any(m => m.UserId == userId))
                    throw new UnauthorizedAccessException("You are not a member of this group");

                var funds = await _context.GroupFunds
                    .Where(gf => gf.GroupID == model.GroupID)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<GroupFundDTO>>(funds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching group funds with GroupID: {GroupID}", model.GroupID);
                throw;
            }
        }

        public async Task<GroupFundDTO> UpdateGroupFundAsync(UpdateGroupFundDTO dto)
        {
            try
            {
                _logger.LogInformation("Updating group fund with ID: {GroupFundID}", dto.GroupFundID);

                var userId = GetCurrentUserId();

                var groupFund = await _context.GroupFunds
                    .Include(gf => gf.Group)
                        .ThenInclude(g => g.Members)
                    .FirstOrDefaultAsync(gf => gf.GroupFundID == dto.GroupFundID);

                if (groupFund == null)
                    throw new Exception("Group fund not found");

                if (!groupFund.Group.Members.Any(m => m.UserId == userId))
                    throw new UnauthorizedAccessException("You are not a member of this group");

                groupFund.Description = dto.Description;
                groupFund.SavingGoal = dto.SavingGoal;
                groupFund.UpdatedAt = DateTime.UtcNow;

                _context.GroupFunds.Update(groupFund);
                await _context.SaveChangesAsync();

                return _mapper.Map<GroupFundDTO>(groupFund);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating group fund: {GroupFundID}", dto.GroupFundID);
                throw;
            }
        }

        public async Task<Guid> DeleteGroupFundAsync(Guid groupFundId)
        {
            try
            {
                _logger.LogInformation("Deleting group fund with ID: {GroupFundID}", groupFundId);

                var userId = GetCurrentUserId();

                var groupFund = await _context.GroupFunds
                    .Include(gf => gf.Group)
                        .ThenInclude(g => g.Members)
                    .FirstOrDefaultAsync(gf => gf.GroupFundID == groupFundId);

                if (groupFund == null)
                    throw new Exception("Group fund not found");

                if (!groupFund.Group.Members.Any(m => m.UserId == userId))
                    throw new UnauthorizedAccessException("You are not a member of this group");

                _context.GroupFunds.Remove(groupFund);
                await _context.SaveChangesAsync();

                return groupFundId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting group fund: {GroupFundID}", groupFundId);                throw;
            }
        }

        /// <summary>
        /// Gets a single GroupFund by its ID
        /// Safe extension method for SignalR notification support
        /// </summary>
        /// <param name="groupFundId">The ID of the GroupFund to retrieve</param>
        /// <returns>The GroupFund data if found and user has access</returns>
        public async Task<GroupFundDTO?> GetGroupFundByIdAsync(Guid groupFundId)
        {
            try
            {
                _logger.LogInformation("Fetching group fund with ID: {GroupFundID}", groupFundId);

                var userId = GetCurrentUserId();

                var groupFund = await _context.GroupFunds
                    .Include(gf => gf.Group)
                        .ThenInclude(g => g.Members)
                    .FirstOrDefaultAsync(gf => gf.GroupFundID == groupFundId);

                if (groupFund == null)
                {
                    _logger.LogWarning("GroupFund not found: {GroupFundID}", groupFundId);
                    return null;
                }

                if (!groupFund.Group.Members.Any(m => m.UserId == userId))
                    throw new UnauthorizedAccessException("You are not a member of this group");

                return _mapper.Map<GroupFundDTO>(groupFund);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching group fund: {GroupFundID}", groupFundId);
                throw;
            }
        }
    }
}
