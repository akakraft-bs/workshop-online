using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface IProjektService
{
    Task<IEnumerable<ProjektDto>> GetAllAsync();
    Task<ProjektDto> CreateAsync(Guid userId, CreateProjektDto dto);
    Task<ProjektDto?> UpdateAsync(Guid id, UpdateProjektDto dto);
    Task<bool> DeleteAsync(Guid id);
}
