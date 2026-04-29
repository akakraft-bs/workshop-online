using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface IVereinZugangService
{
    Task<IEnumerable<VereinZugangDto>> GetAllAsync();
    Task<VereinZugangDto> CreateAsync(CreateVereinZugangDto dto);
    Task<VereinZugangDto?> UpdateAsync(Guid id, UpdateVereinZugangDto dto);
    Task<bool> DeleteAsync(Guid id);
}
