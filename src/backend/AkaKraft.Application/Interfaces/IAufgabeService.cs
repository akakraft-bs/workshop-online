using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface IAufgabeService
{
    Task<IEnumerable<AufgabeDto>> GetAllAsync();
    Task<AufgabeDto> CreateAsync(Guid creatorId, CreateAufgabeDto dto);
    Task<AufgabeDto?> UpdateAsync(Guid id, UpdateAufgabeDto dto);
    Task<bool> DeleteAsync(Guid id);
}
