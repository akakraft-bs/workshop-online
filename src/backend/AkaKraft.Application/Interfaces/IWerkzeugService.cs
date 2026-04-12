using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface IWerkzeugService
{
    Task<IEnumerable<WerkzeugDto>> GetAllAsync();
    Task<WerkzeugDto> CreateAsync(CreateWerkzeugDto dto);
    Task<WerkzeugDto?> UpdateAsync(Guid id, UpdateWerkzeugDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<WerkzeugDto?> AusleihenAsync(Guid id, Guid userId, DateTime expectedReturnAt);
    Task<(WerkzeugDto? Dto, bool Forbidden)> ZurueckgebenAsync(Guid id, Guid userId, bool isPrivileged);
}
