﻿using API.Models.DTOs;
using API.Models.Entities;

namespace API.Repositories
{
    public interface IGroupFundRepository
    {
        Task<GroupFundDTO> CreateGroupFundAsync(CreateGroupFundDTO dto);
        Task<IEnumerable<GroupFundDTO>> GetGroupFundsByGroupIdAsync(GetGroupFundByGroupIdDTO model);
        Task<GroupFundDTO> UpdateGroupFundAsync(UpdateGroupFundDTO dto);
        Task<Guid> DeleteGroupFundAsync(Guid groupFundId);

    }
}
