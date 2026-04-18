using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface IWunschService
{
    Task<IEnumerable<WunschDto>> GetAllAsync(Guid currentUserId);
    Task<WunschDto> CreateAsync(Guid userId, CreateWunschDto dto);
    Task<WunschDto?> VoteAsync(Guid wunschId, Guid userId, bool isUpvote);
    Task<(WunschDto? Dto, bool Forbidden)> UpdateAsync(Guid wunschId, Guid requestingUserId, bool isPrivileged, UpdateWunschDto dto);
    Task<WunschDto?> CloseAsync(Guid wunschId, Guid closedByUserId, CloseWunschDto dto);
}
