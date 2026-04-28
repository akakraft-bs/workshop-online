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
        var feedbacks = await db.Feedbacks
            .Include(f => f.User)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        var userPrefs = await db.UserPreferences
            .Where(p => p.DisplayName != null)
            .ToDictionaryAsync(p => p.UserId, p => p.DisplayName!);

        return feedbacks.Select(f => ToDto(f, userPrefs));
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

        var createPrefs = await db.UserPreferences
            .Where(p => p.UserId == userId && p.DisplayName != null)
            .ToDictionaryAsync(p => p.UserId, p => p.DisplayName!);

        return ToDto(feedback, createPrefs);
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

        var updatePrefs = await db.UserPreferences
            .Where(p => p.UserId == feedback.UserId && p.DisplayName != null)
            .ToDictionaryAsync(p => p.UserId, p => p.DisplayName!);

        return ToDto(feedback, updatePrefs);
    }

    private static FeedbackDto ToDto(Feedback f, Dictionary<Guid, string> prefs)
    {
        var name = prefs.TryGetValue(f.UserId, out var n) ? n : f.User.Name;
        return new FeedbackDto(f.Id, f.UserId, name, f.Text, f.PageUrl, f.Status, f.CreatedAt);
    }
}
