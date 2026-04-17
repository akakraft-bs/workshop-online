using AkaKraft.Domain.Enums;

namespace AkaKraft.Application.DTOs;

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
    string? Note
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
