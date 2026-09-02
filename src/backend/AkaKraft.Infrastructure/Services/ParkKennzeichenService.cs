using System.Collections.Concurrent;
using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Services;

public class ParkKennzeichenService(ApplicationDbContext db, ICampusParkenClient portal) : IParkKennzeichenService
{
    // Serialisiert Änderungen pro Konto (read-modify-write gegen das Portal).
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> Locks = new();

    public async Task<ParkKennzeichenListeDto> GetAsync(Guid accountId, CancellationToken ct = default)
    {
        var account = await db.ParkAccounts.FirstOrDefaultAsync(a => a.Id == accountId, ct)
            ?? throw new KeyNotFoundException("Parkkonto nicht gefunden.");

        if (!HatZugang(account))
            return Leer(account, konfiguriert: false, fehler: null);

        try
        {
            var codes = await portal.ListAsync(account.Id, account.PortalUsername!, account.PortalPassword!, allowCache: true, ct);
            return new ParkKennzeichenListeDto(
                account.Id, account.Label, true, IParkKennzeichenService.MaxKennzeichen,
                codes.Select(Normalize).Distinct().ToList(), null);
        }
        catch (CampusParkenException ex)
        {
            return Leer(account, konfiguriert: true, fehler: ex.Message);
        }
    }

    public Task<ParkKennzeichenListeDto> AddAsync(Guid accountId, Guid userId, string kennzeichen, CancellationToken ct = default)
        => MutateAsync(accountId, userId, kennzeichen, ParkKennzeichenAktion.Hinzugefuegt, ct);

    public Task<ParkKennzeichenListeDto> RemoveAsync(Guid accountId, Guid userId, string kennzeichen, CancellationToken ct = default)
        => MutateAsync(accountId, userId, kennzeichen, ParkKennzeichenAktion.Entfernt, ct);

    public async Task<IReadOnlyList<ParkKennzeichenAuditDto>> GetHistorieAsync(Guid accountId, int limit, CancellationToken ct = default)
    {
        var take = Math.Clamp(limit, 1, 200);
        var rows = await db.ParkKennzeichenAudits
            .Where(a => a.ParkAccountId == accountId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

        return rows.Select(a => new ParkKennzeichenAuditDto(
            a.Id, a.AusgefuehrtVon, a.Aktion.ToString(), a.Kennzeichen,
            SplitListe(a.KennzeichenNachher), a.CreatedAt)).ToList();
    }

    // -------------------------------------------------------------------------

    private async Task<ParkKennzeichenListeDto> MutateAsync(
        Guid accountId, Guid userId, string kennzeichenRaw, ParkKennzeichenAktion aktion, CancellationToken ct)
    {
        var account = await db.ParkAccounts.FirstOrDefaultAsync(a => a.Id == accountId, ct)
            ?? throw new KeyNotFoundException("Parkkonto nicht gefunden.");

        if (!HatZugang(account))
            throw new InvalidOperationException("Für dieses Parkkonto sind keine Portal-Zugangsdaten hinterlegt.");

        var kennzeichen = Normalize(kennzeichenRaw);
        if (kennzeichen.Length is 0 or > 16)
            throw new InvalidOperationException("Ungültiges Kennzeichen.");

        var gate = Locks.GetOrAdd(accountId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            var aktuell = (await portal.ListAsync(account.Id, account.PortalUsername!, account.PortalPassword!, allowCache: false, ct))
                .Select(Normalize).Distinct().ToList();

            List<string> neu;
            if (aktion == ParkKennzeichenAktion.Hinzugefuegt)
            {
                if (aktuell.Contains(kennzeichen))
                    return Erfolg(account, aktuell);
                if (aktuell.Count >= IParkKennzeichenService.MaxKennzeichen)
                    throw new InvalidOperationException(
                        $"Es sind bereits {IParkKennzeichenService.MaxKennzeichen} Kennzeichen hinterlegt. Bitte zuerst eines entfernen.");
                neu = [.. aktuell, kennzeichen];
            }
            else
            {
                if (!aktuell.Contains(kennzeichen))
                    return Erfolg(account, aktuell);
                neu = aktuell.Where(k => k != kennzeichen).ToList();
            }

            await portal.ReplaceAsync(account.Id, account.PortalUsername!, account.PortalPassword!, neu, ct);

            var ausgefuehrtVon = await AnzeigeNameAsync(userId, ct);
            db.ParkKennzeichenAudits.Add(new ParkKennzeichenAudit
            {
                Id = Guid.NewGuid(),
                ParkAccountId = accountId,
                UserId = userId,
                AusgefuehrtVon = ausgefuehrtVon,
                Aktion = aktion,
                Kennzeichen = kennzeichen,
                KennzeichenNachher = string.Join(",", neu),
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(ct);

            return Erfolg(account, neu);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<string> AnzeigeNameAsync(Guid userId, CancellationToken ct)
    {
        var name = await db.UserPreferences
            .Where(p => p.UserId == userId && p.DisplayName != null)
            .Select(p => p.DisplayName!)
            .FirstOrDefaultAsync(ct);
        return name
            ?? (await db.Users.Where(u => u.Id == userId).Select(u => u.Name).FirstOrDefaultAsync(ct))
            ?? "Unbekannt";
    }

    private static bool HatZugang(ParkAccount a) =>
        !string.IsNullOrWhiteSpace(a.PortalUsername) && !string.IsNullOrWhiteSpace(a.PortalPassword);

    private static string Normalize(string raw) =>
        new string((raw ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();

    private static List<string> SplitListe(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static ParkKennzeichenListeDto Leer(ParkAccount a, bool konfiguriert, string? fehler) =>
        new(a.Id, a.Label, konfiguriert, IParkKennzeichenService.MaxKennzeichen, [], fehler);

    private static ParkKennzeichenListeDto Erfolg(ParkAccount a, IReadOnlyList<string> codes) =>
        new(a.Id, a.Label, true, IParkKennzeichenService.MaxKennzeichen, codes, null);
}
