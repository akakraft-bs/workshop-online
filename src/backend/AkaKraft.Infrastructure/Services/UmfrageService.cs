using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Domain.Enums;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Services;

public class UmfrageService(ApplicationDbContext db) : IUmfrageService
{
    public async Task<IEnumerable<UmfrageDto>> GetAllAsync(Guid currentUserId, bool isPrivileged)
    {
        var umfragen = await db.Umfragen
            .Include(u => u.CreatedBy)
            .Include(u => u.ClosedBy)
            .Include(u => u.Options.OrderBy(o => o.SortOrder))
                .ThenInclude(o => o.Antworten)
                    .ThenInclude(a => a.User)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var userPrefs = await LoadPrefsAsync();

        return umfragen.Select(u => BuildDto(u, currentUserId, isPrivileged, userPrefs));
    }

    public async Task<UmfrageDto> CreateAsync(Guid userId, CreateUmfrageDto dto)
    {
        if (dto.Options.Count < 2)
            throw new InvalidOperationException("Mindestens 2 Antwortmöglichkeiten sind erforderlich.");

        var umfrage = new Umfrage
        {
            Id = Guid.NewGuid(),
            Question = dto.Question,
            IsMultipleChoice = dto.IsMultipleChoice,
            ResultsVisible = dto.ResultsVisible,
            RevealAfterClose = dto.RevealAfterClose,
            Deadline = dto.Deadline?.ToUniversalTime(),
            Status = UmfrageStatus.Offen,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
        };

        for (int i = 0; i < dto.Options.Count; i++)
        {
            umfrage.Options.Add(new UmfrageOption
            {
                Id = Guid.NewGuid(),
                UmfrageId = umfrage.Id,
                Text = dto.Options[i],
                SortOrder = i,
            });
        }

        db.Umfragen.Add(umfrage);
        await db.SaveChangesAsync();

        await db.Entry(umfrage).Reference(u => u.CreatedBy).LoadAsync();

        var userPrefs = await LoadPrefsAsync();
        return BuildDto(umfrage, userId, false, userPrefs);
    }

    public async Task<(UmfrageDto? Dto, bool Forbidden)> UpdateAsync(
        Guid umfrageId, Guid requestingUserId, bool isPrivileged, UpdateUmfrageDto dto)
    {
        var umfrage = await db.Umfragen
            .Include(u => u.CreatedBy)
            .Include(u => u.Options.OrderBy(o => o.SortOrder))
                .ThenInclude(o => o.Antworten)
                    .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(u => u.Id == umfrageId);

        if (umfrage is null)
            return (null, false);

        if (!isPrivileged && umfrage.CreatedByUserId != requestingUserId)
            return (null, true);

        if (umfrage.Status == UmfrageStatus.Geschlossen)
            return (null, true);

        umfrage.Question = dto.Question;
        umfrage.IsMultipleChoice = dto.IsMultipleChoice;
        umfrage.ResultsVisible = dto.ResultsVisible;
        umfrage.RevealAfterClose = dto.RevealAfterClose;
        umfrage.Deadline = dto.Deadline?.ToUniversalTime();

        // Update options: keep existing (by Id), add new, remove absent
        var existingOptions = umfrage.Options.ToDictionary(o => o.Id);
        var keptIds = new HashSet<Guid>();

        for (int i = 0; i < dto.Options.Count; i++)
        {
            var optDto = dto.Options[i];
            if (optDto.Id.HasValue && existingOptions.TryGetValue(optDto.Id.Value, out var existing))
            {
                existing.Text = optDto.Text;
                existing.SortOrder = i;
                keptIds.Add(existing.Id);
            }
            else
            {
                var newOpt = new UmfrageOption
                {
                    Id = Guid.NewGuid(),
                    UmfrageId = umfrageId,
                    Text = optDto.Text,
                    SortOrder = i,
                };
                db.UmfrageOptions.Add(newOpt);
                keptIds.Add(newOpt.Id);
            }
        }

        // Remove options not in the new list (cascade deletes answers)
        foreach (var opt in existingOptions.Values.Where(o => !keptIds.Contains(o.Id)))
            db.UmfrageOptions.Remove(opt);

        await db.SaveChangesAsync();

        // Reload options after changes
        await db.Entry(umfrage).Collection(u => u.Options).Query()
            .Include(o => o.Antworten).ThenInclude(a => a.User)
            .LoadAsync();

        var userPrefs = await LoadPrefsAsync();
        return (BuildDto(umfrage, requestingUserId, isPrivileged, userPrefs), false);
    }

    public async Task<(bool Success, bool Forbidden)> DeleteAsync(
        Guid umfrageId, Guid requestingUserId, bool isPrivileged)
    {
        var umfrage = await db.Umfragen.FirstOrDefaultAsync(u => u.Id == umfrageId);

        if (umfrage is null)
            return (false, false);

        if (!isPrivileged && umfrage.CreatedByUserId != requestingUserId)
            return (false, true);

        db.Umfragen.Remove(umfrage);
        await db.SaveChangesAsync();
        return (true, false);
    }

    public async Task<(UmfrageDto? Dto, string? Error)> VoteAsync(
        Guid umfrageId, Guid userId, VoteUmfrageDto dto, bool isPrivileged)
    {
        var umfrage = await db.Umfragen
            .Include(u => u.CreatedBy)
            .Include(u => u.Options.OrderBy(o => o.SortOrder))
            .FirstOrDefaultAsync(u => u.Id == umfrageId);

        if (umfrage is null)
            return (null, "Umfrage nicht gefunden.");

        if (umfrage.Status == UmfrageStatus.Geschlossen)
            return (null, "Diese Umfrage ist bereits geschlossen.");

        // Validate option IDs belong to this poll
        var validOptionIds = umfrage.Options.Select(o => o.Id).ToHashSet();
        if (dto.OptionIds.Any(id => !validOptionIds.Contains(id)))
            return (null, "Ungültige Antwortmöglichkeit.");

        // Enforce single-choice constraint
        if (!umfrage.IsMultipleChoice && dto.OptionIds.Count > 1)
            return (null, "Diese Umfrage erlaubt nur eine Antwort.");

        // Replace existing answers for this user in this poll
        var existing = await db.UmfrageAntworten
            .Where(a => a.UmfrageId == umfrageId && a.UserId == userId)
            .ToListAsync();

        db.UmfrageAntworten.RemoveRange(existing);

        foreach (var optionId in dto.OptionIds)
        {
            db.UmfrageAntworten.Add(new UmfrageAntwort
            {
                Id = Guid.NewGuid(),
                UmfrageId = umfrageId,
                OptionId = optionId,
                UserId = userId,
                VotedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();

        // Reload with full data
        await db.Entry(umfrage).Collection(u => u.Options).Query()
            .Include(o => o.Antworten).ThenInclude(a => a.User)
            .LoadAsync();

        var userPrefs = await LoadPrefsAsync();
        return (BuildDto(umfrage, userId, isPrivileged, userPrefs), null);
    }

    public async Task<(UmfrageDto? Dto, bool Forbidden)> CloseAsync(
        Guid umfrageId, Guid requestingUserId, bool isPrivileged)
    {
        var umfrage = await db.Umfragen
            .Include(u => u.CreatedBy)
            .Include(u => u.Options.OrderBy(o => o.SortOrder))
                .ThenInclude(o => o.Antworten)
                    .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(u => u.Id == umfrageId);

        if (umfrage is null)
            return (null, false);

        if (!isPrivileged && umfrage.CreatedByUserId != requestingUserId)
            return (null, true);

        umfrage.Status = UmfrageStatus.Geschlossen;
        umfrage.ClosedByUserId = requestingUserId;
        umfrage.ClosedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await db.Entry(umfrage).Reference(u => u.ClosedBy).LoadAsync();

        var userPrefs = await LoadPrefsAsync();
        return (BuildDto(umfrage, requestingUserId, isPrivileged, userPrefs), false);
    }

    // -------------------------------------------------------------------------

    private async Task<Dictionary<Guid, string>> LoadPrefsAsync() =>
        await db.UserPreferences
            .Where(p => p.DisplayName != null)
            .ToDictionaryAsync(p => p.UserId, p => p.DisplayName!);

    private static bool CanSeeResults(Umfrage umfrage, Guid currentUserId, bool isPrivileged)
    {
        if (isPrivileged || umfrage.CreatedByUserId == currentUserId)
            return true;

        if (umfrage.ResultsVisible)
            return true;

        if (umfrage.Status == UmfrageStatus.Geschlossen && umfrage.RevealAfterClose)
            return true;

        return false;
    }

    private static UmfrageDto BuildDto(
        Umfrage umfrage,
        Guid currentUserId,
        bool isPrivileged,
        Dictionary<Guid, string> prefs)
    {
        string DisplayName(Guid uid, string fallback) =>
            prefs.TryGetValue(uid, out var name) ? name : fallback;

        var showResults = CanSeeResults(umfrage, currentUserId, isPrivileged);

        // Distinct participants (users who answered at least one option)
        var participantCount = umfrage.Options
            .SelectMany(o => o.Antworten)
            .Select(a => a.UserId)
            .Distinct()
            .Count();

        var currentUserOptionIds = umfrage.Options
            .SelectMany(o => o.Antworten)
            .Where(a => a.UserId == currentUserId)
            .Select(a => a.OptionId)
            .ToList();

        var options = umfrage.Options
            .OrderBy(o => o.SortOrder)
            .Select(o =>
            {
                var voters = showResults
                    ? o.Antworten.Select(a => DisplayName(a.UserId, a.User.Name)).ToList()
                    : null;
                var voteCount = showResults ? o.Antworten.Count : (int?)null;
                return new UmfrageOptionDto(o.Id, o.Text, o.SortOrder, voteCount, voters);
            })
            .ToList();

        return new UmfrageDto(
            umfrage.Id,
            umfrage.Question,
            umfrage.IsMultipleChoice,
            umfrage.ResultsVisible,
            umfrage.RevealAfterClose,
            umfrage.Deadline,
            umfrage.Status,
            umfrage.CreatedByUserId,
            DisplayName(umfrage.CreatedByUserId, umfrage.CreatedBy.Name),
            umfrage.CreatedAt,
            umfrage.ClosedByUserId,
            umfrage.ClosedBy is null ? null : DisplayName(umfrage.ClosedByUserId!.Value, umfrage.ClosedBy.Name),
            umfrage.ClosedAt,
            options,
            currentUserOptionIds,
            participantCount);
    }
}
