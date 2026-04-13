using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Domain.Enums;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Services;

public class FeedbackService(ApplicationDbContext db) : IFeedbackService
{
    public async Task<IEnumerable<FeedbackDto>> GetAllAsync()
    {
        return await db.Feedbacks
            .Include(f => f.User)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FeedbackDto(
                f.Id,
                f.UserId,
                f.User.Name,
                f.Text,
                f.PageUrl,
                f.Status,
                f.CreatedAt))
            .ToListAsync();
    }

    public async Task<FeedbackDto> CreateAsync(Guid userId, CreateFeedbackDto dto)
    {
        var feedback = new Feedback
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Text = dto.Text,
            PageUrl = dto.PageUrl,
            Status = FeedbackStatus.New,
            CreatedAt = DateTime.UtcNow,
        };

        db.Feedbacks.Add(feedback);
        await db.SaveChangesAsync();

        await db.Entry(feedback).Reference(f => f.User).LoadAsync();

        return new FeedbackDto(
            feedback.Id,
            feedback.UserId,
            feedback.User.Name,
            feedback.Text,
            feedback.PageUrl,
            feedback.Status,
            feedback.CreatedAt);
    }

    public async Task<FeedbackDto?> UpdateStatusAsync(Guid id, FeedbackStatus status)
    {
        var feedback = await db.Feedbacks
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (feedback is null)
            return null;

        feedback.Status = status;
        await db.SaveChangesAsync();

        return new FeedbackDto(
            feedback.Id,
            feedback.UserId,
            feedback.User.Name,
            feedback.Text,
            feedback.PageUrl,
            feedback.Status,
            feedback.CreatedAt);
    }
}
