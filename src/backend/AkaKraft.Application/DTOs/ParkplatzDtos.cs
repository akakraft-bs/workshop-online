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
    ParkClaimDto? Belegung,
    bool ZugangKonfiguriert = false,
    IReadOnlyList<string>? Kennzeichen = null,
    string? KennzeichenFehler = null
);

public record ParkAccountAdminDto(
    Guid Id,
    string Label,
    string? PortalUrl,
    string? Notiz,
    string? PortalUsername,
    bool ZugangKonfiguriert,
    int SortOrder
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

/// <summary>
/// Ein Eintrag im gemeinsamen Verlauf: entweder eine Konto-Belegung oder eine
/// Kennzeichen-Änderung. <see cref="Typ"/> = "Belegung" | "KennzeichenHinzugefuegt" | "KennzeichenEntfernt".
/// </summary>
public record ParkHistorieDto(
    string Id,
    string Typ,
    DateTime Zeitpunkt,
    string AccountLabel,
    string DisplayName,
    // Belegung
    string? Kennzeichen,
    string? FahrzeugBezeichnung,
    DateTime? EinfahrtAt,
    DateTime? FreigegebenAt,
    DateTime? AutoExpiresAt,
    string? BerechtigungArt,
    string? BestaetigungHinweis,
    string? Status
);

public record ParkClaimUpdateRequest(DateTime? VoraussichtlichBis);

public record ParkAccountUpdateRequest(string Label, string? PortalUrl, string? Notiz);

// ---- Kennzeichen-Verwaltung über die Bewirtschafter-API ----

public record ParkKennzeichenListeDto(
    Guid AccountId,
    string AccountLabel,
    bool ZugangKonfiguriert,
    int Max,
    IReadOnlyList<string> Kennzeichen,
    string? Fehler
);

public record ParkKennzeichenAddRequest(string Kennzeichen);

public record ParkKennzeichenAuditDto(
    Guid Id,
    string AusgefuehrtVon,
    string Aktion,
    string Kennzeichen,
    IReadOnlyList<string> KennzeichenNachher,
    DateTime CreatedAt
);

/// <summary>Zugangsdaten setzen. Username null = unverändert; Password null = unverändert, "" = löschen.</summary>
public record ParkZugangUpdateRequest(string? Username, string? Password);

public record ParkZugangStatusDto(Guid AccountId, bool ZugangKonfiguriert, string? Username);
