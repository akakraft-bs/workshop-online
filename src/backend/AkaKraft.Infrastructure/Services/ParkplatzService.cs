using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Domain.Enums;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Services;

public class ParkplatzService(ApplicationDbContext db, ICalendarService calendarService) : IParkplatzService
{
    // Berechtigungsfenster rund um eine Bühne-Halle-1-Reservierung.
    private static readonly TimeSpan VorlaufBerechtigung = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan NachlaufBerechtigung = TimeSpan.FromHours(2);

    public async Task<ParkOverviewDto> GetOverviewAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var accounts = await db.ParkAccounts
            .OrderBy(a => a.SortOrder).ThenBy(a => a.Label)
            .ToListAsync(ct);

        var accountIds = accounts.Select(a => a.Id).ToList();

        var aktiveClaims = await db.ParkClaims
            .Include(c => c.User)
            .Where(c => accountIds.Contains(c.ParkAccountId)
                     && c.FreigegebenAt == null
                     && c.AutoExpiresAt > now)
            .ToListAsync(ct);

        var displayNames = await LoadDisplayNamesAsync(aktiveClaims.Select(c => c.UserId), ct);

        var accountDtos = accounts.Select(a =>
        {
            var claim = aktiveClaims.FirstOrDefault(c => c.ParkAccountId == a.Id);
            return new ParkAccountStatusDto(
                a.Id, a.Label, a.PortalUrl, a.Notiz,
                IstFrei: claim is null,
                Belegung: claim is null ? null : ToClaimDto(claim, displayNames));
        }).ToList();

        var parkCalendarIds = await GetParkCalendarIdsAsync(ct);
        var kalenderKonfiguriert = parkCalendarIds.Count > 0;

        var berechtigung = await GetBerechtigungInternalAsync(userId, now, parkCalendarIds, ct);
        var anstehende = await GetAnstehendeReservierungenAsync(userId, now, parkCalendarIds, ct);

        return new ParkOverviewDto(accountDtos, berechtigung, anstehende, kalenderKonfiguriert);
    }

    public async Task<ParkBerechtigungDto> GetBerechtigungAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default)
    {
        var parkCalendarIds = await GetParkCalendarIdsAsync(ct);
        return await GetBerechtigungInternalAsync(userId, nowUtc, parkCalendarIds, ct);
    }

    // -------------------------------------------------------------------------

    private async Task<ParkBerechtigungDto> GetBerechtigungInternalAsync(
        Guid userId, DateTime now, List<string> parkCalendarIds, CancellationToken ct)
    {
        if (parkCalendarIds.Count == 0)
        {
            return new ParkBerechtigungDto(
                nameof(ParkBerechtigungArt.Spontan),
                ErfordertBestaetigung: true,
                "Es ist noch kein Bühne-Halle-1-Kalender hinterlegt. Du kannst ein freies Konto trotzdem nutzen, wenn es niemand mit Reservierung braucht – bitte im Vorstand melden.",
                []);
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        var userEmail = user?.Email ?? string.Empty;

        var events = (await calendarService.GetEventsAsync(
                parkCalendarIds, now.AddHours(-3), now.AddHours(24)))
            .Where(e => e.Start.HasValue)
            .ToList();

        bool ImFenster(CalendarEventDto e)
        {
            var start = e.Start!.Value;
            var end = e.End ?? start;
            return now >= start - VorlaufBerechtigung && now <= end + NachlaufBerechtigung;
        }

        bool IstMeine(CalendarEventDto e) =>
            e.CreatorUserId == userId
            || (!string.IsNullOrEmpty(userEmail)
                && string.Equals(e.CreatorEmail, userEmail, StringComparison.OrdinalIgnoreCase));

        var fensterEvents = events.Where(ImFenster).ToList();
        var meineFensterEvents = fensterEvents.Where(IstMeine).ToList();

        if (meineFensterEvents.Count > 0)
        {
            var ev = meineFensterEvents[0];
            return new ParkBerechtigungDto(
                nameof(ParkBerechtigungArt.Automatisch),
                ErfordertBestaetigung: false,
                $"Reservierung „{ev.Title}“ erkannt – du kannst direkt einchecken.",
                [],
                ev.FahrzeugId,
                ev.FahrzeugLabel);
        }

        if (fensterEvents.Count > 0)
        {
            return new ParkBerechtigungDto(
                nameof(ParkBerechtigungArt.Selbstbestaetigt),
                ErfordertBestaetigung: true,
                "Wir konnten dir keine Bühne-Halle-1-Reservierung sicher zuordnen. Bitte bestätige, dass du für die aktuelle Bühnen-Nutzung fährst – wähle bei Bedarf die passende Reservierung aus.",
                fensterEvents.Select(e => ToReservierungDto(e, IstMeine(e))).ToList());
        }

        // Keine Reservierung im Fenster – aber vielleicht eine eigene, die erst später beginnt.
        var meineKommende = events
            .Where(e => IstMeine(e) && e.Start!.Value > now)
            .OrderBy(e => e.Start)
            .FirstOrDefault();

        if (meineKommende is not null)
        {
            var lokal = ToBerlin(meineKommende.Start!.Value);
            return new ParkBerechtigungDto(
                nameof(ParkBerechtigungArt.Spontan),
                ErfordertBestaetigung: true,
                $"Deine Reservierung „{meineKommende.Title}“ beginnt erst am {lokal:dd.MM. HH:mm} Uhr – das Berechtigungsfenster öffnet 30 Min. vorher. Du kannst das Konto jetzt schon spontan übernehmen; bitte bei Andrang zügig wieder freigeben.",
                [],
                meineKommende.FahrzeugId,
                meineKommende.FahrzeugLabel);
        }

        return new ParkBerechtigungDto(
            nameof(ParkBerechtigungArt.Spontan),
            ErfordertBestaetigung: true,
            "Aktuell ist keine Bühne-Halle-1-Reservierung eingetragen. Du darfst ein freies Konto spontan nutzen – bitte bei Andrang zügig wieder freigeben.",
            []);
    }

    private async Task<List<ParkReservierungDto>> GetAnstehendeReservierungenAsync(
        Guid userId, DateTime now, List<string> parkCalendarIds, CancellationToken ct)
    {
        if (parkCalendarIds.Count == 0) return [];

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        var userEmail = user?.Email ?? string.Empty;

        var events = await calendarService.GetEventsAsync(parkCalendarIds, now.AddHours(-2), now.AddHours(24));

        return events
            .Where(e => e.Start.HasValue)
            .OrderBy(e => e.Start)
            .Take(12)
            .Select(e => ToReservierungDto(
                e,
                e.CreatorUserId == userId
                || (!string.IsNullOrEmpty(userEmail)
                    && string.Equals(e.CreatorEmail, userEmail, StringComparison.OrdinalIgnoreCase))))
            .ToList();
    }

    private async Task<List<string>> GetParkCalendarIdsAsync(CancellationToken ct) =>
        await db.CalendarConfigs
            .Where(c => c.GrantsParkplatzBerechtigung)
            .Select(c => c.GoogleCalendarId)
            .ToListAsync(ct);

    private async Task<Dictionary<Guid, string>> LoadDisplayNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<Guid, string>();

        return await db.UserPreferences
            .Where(p => ids.Contains(p.UserId) && p.DisplayName != null)
            .ToDictionaryAsync(p => p.UserId, p => p.DisplayName!, ct);
    }

    private static ParkClaimDto ToClaimDto(ParkClaim c, IReadOnlyDictionary<Guid, string> displayNames) =>
        new(
            c.Id,
            c.ParkAccountId,
            c.UserId,
            displayNames.GetValueOrDefault(c.UserId) ?? c.User.Name,
            c.User.PictureUrl,
            c.Kennzeichen,
            c.FahrzeugBezeichnung,
            c.EinfahrtAt,
            c.VoraussichtlichBis,
            c.AutoExpiresAt,
            c.BerechtigungArt.ToString());

    private static ParkReservierungDto ToReservierungDto(CalendarEventDto e, bool istMeine) =>
        new(e.Id, e.Title, e.Start, e.End, istMeine, e.FahrzeugId, e.FahrzeugLabel);

    private static readonly TimeZoneInfo BerlinZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    private static DateTime ToBerlin(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), BerlinZone);
}
