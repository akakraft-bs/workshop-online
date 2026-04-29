using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface IDokumenteService
{
    Task<IEnumerable<DokumentOrdnerDto>> GetAllAsync();
    Task<DokumentOrdnerDto> CreateOrdnerAsync(Guid userId, CreateOrdnerDto dto);
    Task<bool> DeleteOrdnerAsync(Guid id);
    Task<DokumentDto> CreateDokumentAsync(Guid userId, CreateDokumentDto dto);
    Task<bool> DeleteDokumentAsync(Guid id);
}
