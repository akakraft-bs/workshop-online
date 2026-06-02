using AkaKraft.Domain.Enums;

namespace AkaKraft.Application.DTOs;

public record PartnerOverviewDto(
    Guid Id,
    string Name,
    string? Kategorie,
    PartnerStatus Status,
    string? Website,
    int AnzahlKontakte,
    DateTime? LetzterKontakt
);

public record PartnerDetailDto(
    Guid Id,
    string Name,
    string? Kategorie,
    PartnerStatus Status,
    string? Website,
    string? Notizen,
    IEnumerable<AnsprechpartnerDto> Ansprechpartner,
    IEnumerable<KontakteintragDto> Kontakteintraege
);

public record AnsprechpartnerDto(
    Guid Id,
    string Name,
    string? Position,
    string? Email,
    string? Telefon,
    string? Notizen
);

public record KontakteintragDto(
    Guid Id,
    Guid? AnsprechpartnerId,
    string? AnsprechpartnerName,
    DateTime Datum,
    KontaktKanal Kanal,
    KontaktReaktion Reaktion,
    string Zusammenfassung,
    string? NaechsteSchritte,
    DateTime ErstelltAm
);

public record CreatePartnerDto(
    string Name,
    string? Kategorie,
    PartnerStatus Status,
    string? Website,
    string? Notizen
);

public record UpdatePartnerDto(
    string Name,
    string? Kategorie,
    PartnerStatus Status,
    string? Website,
    string? Notizen
);

public record CreateAnsprechpartnerDto(
    string Name,
    string? Position,
    string? Email,
    string? Telefon,
    string? Notizen
);

public record CreateKontakteintragDto(
    Guid? AnsprechpartnerId,
    DateTime Datum,
    KontaktKanal Kanal,
    KontaktReaktion Reaktion,
    string Zusammenfassung,
    string? NaechsteSchritte
);
