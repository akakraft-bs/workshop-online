namespace AkaKraft.Application.DTOs;

/// <summary>Gesamtzustand der Parkkonto-Verwaltung für einen Nutzer.</summary>
public record ParkOverviewDto(
    IReadOnlyList<ParkAccountStatusDto> Accounts,
    ParkBerechtigungDto Berechtigung,
    IReadOnlyList<ParkReservierungDto> AnstehendeReservierungen,
    bool KalenderKonfiguriert
);

public record ParkAccountStatusDto(
    Guid Id,
    string Label,
    string? PortalUrl,
    string? Notiz,
    bool IstFrei,
    ParkClaimDto? Belegung
);

public record ParkClaimDto(
    Guid Id,
    Guid ParkAccountId,
    Guid UserId,
    string DisplayName,
    string? PictureUrl,
    string Kennzeichen,
    string FahrzeugBezeichnung,
    DateTime EinfahrtAt,
    DateTime? VoraussichtlichBis,
    DateTime AutoExpiresAt,
    string BerechtigungArt
);

public record ParkReservierungDto(
    string EventId,
    string Titel,
    DateTime? Start,
    DateTime? End,
    bool IstMeine,
    Guid? FahrzeugId = null,
    string? FahrzeugLabel = null
);

/// <summary>
/// Ergebnis der Berechtigungsprüfung. Es gibt immer einen Weg zum Check-in –
/// bei fehlender sicherer Zuordnung nur mit zusätzlicher Bestätigung.
/// </summary>
public record ParkBerechtigungDto(
    string Art,                  // "Automatisch" | "Selbstbestaetigt" | "Spontan"
    bool ErfordertBestaetigung,
    string Hinweis,
    IReadOnlyList<ParkReservierungDto> WaehlbareReservierungen,
    Guid? VorgeschlagenesFahrzeugId = null,
    string? VorgeschlagenesFahrzeugLabel = null
);

public record ParkCheckinRequest(
    Guid ParkAccountId,
    Guid? FahrzeugId,
    string? Kennzeichen,
    string? FahrzeugBezeichnung,
    DateTime? EinfahrtAt,
    DateTime? VoraussichtlichBis,
    bool BestaetigungAkzeptiert,
    string? BookingEventId
);

public record ParkHistorieEintragDto(
    Guid Id,
    string AccountLabel,
    string DisplayName,
    string Kennzeichen,
    string FahrzeugBezeichnung,
    DateTime EinfahrtAt,
    DateTime? FreigegebenAt,
    DateTime AutoExpiresAt,
    string BerechtigungArt,
    string? BestaetigungHinweis,
    string? BookingEventId,
    string Status
);

public record ParkClaimUpdateRequest(DateTime? VoraussichtlichBis);

public record ParkAccountUpdateRequest(string Label, string? PortalUrl, string? Notiz);
