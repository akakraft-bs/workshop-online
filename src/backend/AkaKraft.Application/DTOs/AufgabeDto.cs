namespace AkaKraft.Application.DTOs;

public record AufgabeDto(
    Guid Id,
    string Titel,
    string Beschreibung,
    string? FotoUrl,
    string Status,
    int Priority,
    Guid? AssignedUserId,
    string? AssignedDisplayName,
    string? AssignedName,
    string CreatedByName,
    DateTime CreatedAt
);

public record CreateAufgabeDto(
    string Titel,
    string Beschreibung,
    string? FotoUrl,
    int Priority = 3
);

public record UpdateAufgabeDto(
    string Titel,
    string Beschreibung,
    string? FotoUrl,
    int Priority,
    Guid? AssignedUserId,
    string? AssignedName,
    bool Erledigt
);
