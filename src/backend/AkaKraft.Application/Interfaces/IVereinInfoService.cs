using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface IVereinInfoService
{
    Task<VereinInfoDto> GetAsync();
    Task<SchluesselhinterlegungDto> CreateSchluesselhinterlegungAsync(CreateSchluesselhinterlegungDto dto);
    Task<SchluesselhinterlegungDto?> UpdateSchluesselhinterlegungAsync(Guid id, UpdateSchluesselhinterlegungDto dto);
    Task<bool> DeleteSchluesselhinterlegungAsync(Guid id);
}
