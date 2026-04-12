using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface IVerbrauchsmaterialService
{
    Task<IEnumerable<VerbrauchsmaterialDto>> GetAllAsync();
}
