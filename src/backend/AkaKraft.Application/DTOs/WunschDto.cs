using AkaKraft.Domain.Enums;

namespace AkaKraft.Application.DTOs;

public record WunschDto(
    Guid Id,
    string Title,
    string Description,
    string? Link,
    WunschStatus Status,
    Guid CreatedByUserId,
    string CreatedByName,
    DateTime CreatedAt,
    int UpVotes,
    int DownVotes,
    bool? CurrentUserVote,   // true = up, false = down, null = no vote
    Guid? ClosedByUserId,
    string? ClosedByName,
    DateTime? ClosedAt,
    string? CloseNote
);

public record CreateWunschDto(
    string Title,
    string Description,
    string? Link
);

public record UpdateWunschDto(
    string Title,
    string Description,
    string? Link
);

public record VoteWunschDto(bool IsUpvote);

public record CloseWunschDto(
    WunschStatus Status,   // Angeschafft or Abgelehnt
    string? CloseNote
);
