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

        //public async Task<GroupFundDTO> UpdateGroupFundAsync(UpdateGroupFundDTO model)
        //{
        //    try
        //    {
        //        _logger.LogInformation("Updating group fund with ID: {GroupFundID}", model.GroupFundID);

        //        var userId = GetCurrentUserId();
        //        var fund = await _context.GroupFunds
        //            .FirstOrDefaultAsync(f => f.GroupFundID == model.GroupFundID && f.UserID == userId);

        //        if (fund == null)
        //            throw new Exception("Group fund not found or you do not have permission to update it");

        //        _mapper.Map(model, fund);
        //        _context.GroupFunds.Update(fund);
        //        await _context.SaveChangesAsync();

        //        return _mapper.Map<GroupFundDTO>(fund);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error occurred while updating group fund: {GroupFundID}", model.GroupFundID);
        //        throw;
        //    }
        //}
    }
}
