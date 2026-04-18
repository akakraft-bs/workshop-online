using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Domain.Enums;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Services;

public class WunschService(ApplicationDbContext db) : IWunschService
{
    public async Task<IEnumerable<WunschDto>> GetAllAsync(Guid currentUserId)
    {
        var wuensche = await db.Wuensche
            .Include(w => w.CreatedBy)
            .Include(w => w.ClosedBy)
            .Include(w => w.Votes)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        var userPrefs = await db.UserPreferences
            .Where(p => p.DisplayName != null)
            .ToDictionaryAsync(p => p.UserId, p => p.DisplayName!);

        string DisplayName(Guid userId, string fallback) =>
            userPrefs.TryGetValue(userId, out var name) ? name : fallback;

        return wuensche.Select(w => new WunschDto(
            w.Id,
            w.Title,
            w.Description,
            w.Link,
            w.Status,
            w.CreatedByUserId,
            DisplayName(w.CreatedByUserId, w.CreatedBy.Name),
            w.CreatedAt,
            w.Votes.Count(v => v.IsUpvote),
            w.Votes.Count(v => !v.IsUpvote),
            w.Votes.FirstOrDefault(v => v.UserId == currentUserId)?.IsUpvote,
            w.ClosedByUserId,
            w.ClosedBy is null ? null : DisplayName(w.ClosedByUserId!.Value, w.ClosedBy.Name),
            w.ClosedAt,
            w.CloseNote));
    }

    public async Task<WunschDto> CreateAsync(Guid userId, CreateWunschDto dto)
    {
        var wunsch = new Wunsch
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            Link = dto.Link,
            Status = WunschStatus.Offen,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
        };

        // Auto-upvote by creator
        wunsch.Votes.Add(new WunschVote
        {
            Id = Guid.NewGuid(),
            WunschId = wunsch.Id,
            UserId = userId,
            IsUpvote = true,
            VotedAt = DateTime.UtcNow,
        });

        db.Wuensche.Add(wunsch);
        await db.SaveChangesAsync();

        await db.Entry(wunsch).Reference(w => w.CreatedBy).LoadAsync();

        var displayName = await db.UserPreferences
            .Where(p => p.UserId == userId && p.DisplayName != null)
            .Select(p => p.DisplayName!)
            .FirstOrDefaultAsync() ?? wunsch.CreatedBy.Name;

        return new WunschDto(
            wunsch.Id,
            wunsch.Title,
            wunsch.Description,
            wunsch.Link,
            wunsch.Status,
            wunsch.CreatedByUserId,
            displayName,
            wunsch.CreatedAt,
            1, 0, true,
            null, null, null, null);
    }

    public async Task<WunschDto?> VoteAsync(Guid wunschId, Guid userId, bool isUpvote)
    {
        var wunsch = await db.Wuensche
            .Include(w => w.CreatedBy)
            .FirstOrDefaultAsync(w => w.Id == wunschId);

        if (wunsch is null || wunsch.Status != WunschStatus.Offen)
            return null;

        // Direkt über DbSet abfragen – kein Collection-Tracking auf dem Wunsch
        var existing = await db.WunschVotes
            .FirstOrDefaultAsync(v => v.WunschId == wunschId && v.UserId == userId);

        if (existing is not null)
        {
            if (existing.IsUpvote == isUpvote)
            {
                // Same vote → toggle off, but prevent creator from removing their auto-upvote
                if (wunsch.CreatedByUserId == userId)
                {
                    // Creator cannot remove their own auto-upvote: return current state
                    var votesNow = await db.WunschVotes.Where(v => v.WunschId == wunschId).ToListAsync();
                    return BuildDto(wunsch, votesNow, userId, await LoadPrefs());
                }

                db.WunschVotes.Remove(existing);
            }
            else
            {
                // Different vote → change
                existing.IsUpvote = isUpvote;
                existing.VotedAt = DateTime.UtcNow;
            }
        }
        else
        {
            db.WunschVotes.Add(new WunschVote
            {
                Id = Guid.NewGuid(),
                WunschId = wunschId,
                UserId = userId,
                IsUpvote = isUpvote,
                VotedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();

        var votes = await db.WunschVotes.Where(v => v.WunschId == wunschId).ToListAsync();
        return BuildDto(wunsch, votes, userId, await LoadPrefs());

        async Task<Dictionary<Guid, string>> LoadPrefs() =>
            await db.UserPreferences
                .Where(p => p.DisplayName != null)
                .ToDictionaryAsync(p => p.UserId, p => p.DisplayName!);
    }

    private static WunschDto BuildDto(
        Wunsch wunsch,
        IList<WunschVote> votes,
        Guid currentUserId,
        Dictionary<Guid, string> prefs)
    {
        string DisplayName(Guid uid, string fallback) =>
            prefs.TryGetValue(uid, out var name) ? name : fallback;

        return new WunschDto(
            wunsch.Id,
            wunsch.Title,
            wunsch.Description,
            wunsch.Link,
            wunsch.Status,
            wunsch.CreatedByUserId,
            DisplayName(wunsch.CreatedByUserId, wunsch.CreatedBy.Name),
            wunsch.CreatedAt,
            votes.Count(v => v.IsUpvote),
            votes.Count(v => !v.IsUpvote),
            votes.FirstOrDefault(v => v.UserId == currentUserId)?.IsUpvote,
            wunsch.ClosedByUserId,
            wunsch.ClosedBy is null ? null : DisplayName(wunsch.ClosedByUserId!.Value, wunsch.ClosedBy.Name),
            wunsch.ClosedAt,
            wunsch.CloseNote);
    }

    public async Task<(WunschDto? Dto, bool Forbidden)> UpdateAsync(
        Guid wunschId, Guid requestingUserId, bool isPrivileged, UpdateWunschDto dto)
    {
        var wunsch = await db.Wuensche
            .Include(w => w.CreatedBy)
            .Include(w => w.Votes)
            .FirstOrDefaultAsync(w => w.Id == wunschId);

        if (wunsch is null)
            return (null, false);

        if (!isPrivileged && wunsch.CreatedByUserId != requestingUserId)
            return (null, true);

        wunsch.Title = dto.Title;
        wunsch.Description = dto.Description;
        wunsch.Link = dto.Link;
        await db.SaveChangesAsync();

        var userPrefs = await db.UserPreferences
            .Where(p => p.DisplayName != null)
            .ToDictionaryAsync(p => p.UserId, p => p.DisplayName!);

        string DisplayName(Guid uid, string fallback) =>
            userPrefs.TryGetValue(uid, out var name) ? name : fallback;

        var currentVote = wunsch.Votes.FirstOrDefault(v => v.UserId == requestingUserId)?.IsUpvote;

        return (new WunschDto(
            wunsch.Id,
            wunsch.Title,
            wunsch.Description,
            wunsch.Link,
            wunsch.Status,
            wunsch.CreatedByUserId,
            DisplayName(wunsch.CreatedByUserId, wunsch.CreatedBy.Name),
            wunsch.CreatedAt,
            wunsch.Votes.Count(v => v.IsUpvote),
            wunsch.Votes.Count(v => !v.IsUpvote),
            currentVote,
            wunsch.ClosedByUserId, null, wunsch.ClosedAt, wunsch.CloseNote), false);
    }

    public async Task<WunschDto?> CloseAsync(Guid wunschId, Guid closedByUserId, CloseWunschDto dto)
    {
        var wunsch = await db.Wuensche
            .Include(w => w.CreatedBy)
            .Include(w => w.Votes)
            .FirstOrDefaultAsync(w => w.Id == wunschId);

        if (wunsch is null)
            return null;

        wunsch.Status = dto.Status;
        wunsch.CloseNote = dto.CloseNote;
        wunsch.ClosedByUserId = closedByUserId;
        wunsch.ClosedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await db.Entry(wunsch).Reference(w => w.ClosedBy).LoadAsync();

        var userPrefs = await db.UserPreferences
            .Where(p => p.DisplayName != null)
            .ToDictionaryAsync(p => p.UserId, p => p.DisplayName!);

        string DisplayName(Guid uid, string fallback) =>
            userPrefs.TryGetValue(uid, out var name) ? name : fallback;

        return new WunschDto(
            wunsch.Id,
            wunsch.Title,
            wunsch.Description,
            wunsch.Link,
            wunsch.Status,
            wunsch.CreatedByUserId,
            DisplayName(wunsch.CreatedByUserId, wunsch.CreatedBy.Name),
            wunsch.CreatedAt,
            wunsch.Votes.Count(v => v.IsUpvote),
            wunsch.Votes.Count(v => !v.IsUpvote),
            null,  // currentUserVote not needed after close
            wunsch.ClosedByUserId,
            DisplayName(closedByUserId, wunsch.ClosedBy!.Name),
            wunsch.ClosedAt,
            wunsch.CloseNote);
    }

}
