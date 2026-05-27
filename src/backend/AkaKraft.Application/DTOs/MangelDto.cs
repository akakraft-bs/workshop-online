using AkaKraft.Domain.Enums;

namespace AkaKraft.Application.DTOs;

public record MangelAnmerkungDto(
    Guid Id,
    string Text,
    Guid CreatedByUserId,
    string CreatedByName,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateMangelAnmerkungDto(string Text);
public record UpdateMangelAnmerkungDto(string Text);

public record MangelDto(
    Guid Id,
    string Title,
    string Description,
    MangelKategorie Kategorie,
    MangelStatus Status,
    Guid CreatedByUserId,
    string CreatedByName,
    DateTime CreatedAt,
    string? ImageUrl,
    Guid? ResolvedByUserId,
    string? ResolvedByName,
    DateTime? ResolvedAt,
    string? Note,
    List<MangelAnmerkungDto> Anmerkungen
);

public record CreateMangelDto(
    string Title,
    string Description,
    MangelKategorie Kategorie,
    string? ImageUrl
);

public record UpdateMangelStatusDto(
    MangelStatus Status,
    string? Note
);

public record UpdateMangelContentDto(
    string Title,
    string Description,
    MangelKategorie Kategorie,
    string? ImageUrl
);
