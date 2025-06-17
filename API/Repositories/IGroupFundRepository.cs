using API.Models.DTOs;
using API.Models.Entities;

namespace API.Repositories
{    public interface IGroupFundRepository
    {
        Task<GroupFundDTO> CreateGroupFundAsync(CreateGroupFundDTO dto);
        Task<IEnumerable<GroupFundDTO>> GetGroupFundsByGroupIdAsync(GetGroupFundByGroupIdDTO model);
        Task<GroupFundDTO> UpdateGroupFundAsync(UpdateGroupFundDTO dto);
        Task<Guid> DeleteGroupFundAsync(Guid groupFundId);
        
        /// <summary>
        /// Gets a single GroupFund by its ID
        /// Safe extension method for SignalR notification support
        /// </summary>
        /// <param name="groupFundId">The ID of the GroupFund to retrieve</param>
        /// <returns>The GroupFund data if found and user has access</returns>
        Task<GroupFundDTO?> GetGroupFundByIdAsync(Guid groupFundId);
    }
}
