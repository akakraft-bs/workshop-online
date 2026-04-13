using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface IVerbrauchsmaterialService
{
    Task<IEnumerable<VerbrauchsmaterialDto>> GetAllAsync();
    Task<VerbrauchsmaterialDto> CreateAsync(CreateVerbrauchsmaterialDto dto);
    Task<bool> DeleteAsync(Guid id);
}
