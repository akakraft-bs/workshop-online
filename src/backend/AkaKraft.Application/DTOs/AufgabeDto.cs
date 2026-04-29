namespace AkaKraft.Application.DTOs;

public record AufgabeDto(
    Guid Id,
    string Titel,
    string Beschreibung,
    string? FotoUrl,
    string Status,
    Guid? AssignedUserId,
    string? AssignedDisplayName,
    string? AssignedName,
    string CreatedByName,
    DateTime CreatedAt
);

public record CreateAufgabeDto(
    string Titel,
    string Beschreibung,
    string? FotoUrl
);

public record UpdateAufgabeDto(
    string Titel,
    string Beschreibung,
    string? FotoUrl,
    Guid? AssignedUserId,
    string? AssignedName,
    bool Erledigt
);
