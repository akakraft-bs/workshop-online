using AkaKraft.Application.DTOs;
using AkaKraft.Domain.Enums;

namespace AkaKraft.Application.Interfaces;

public interface IFeedbackService
{
    Task<IEnumerable<FeedbackDto>> GetAllAsync();
    Task<FeedbackDto> CreateAsync(Guid userId, CreateFeedbackDto dto);
    Task<FeedbackDto?> UpdateStatusAsync(Guid id, FeedbackStatus status);
}
