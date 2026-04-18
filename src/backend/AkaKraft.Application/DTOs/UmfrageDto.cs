using AkaKraft.Domain.Enums;

namespace AkaKraft.Application.DTOs;

public record UmfrageOptionDto(
    Guid Id,
    string Text,
    int SortOrder,
    /// <summary>Null when results are hidden for the requesting user.</summary>
    int? VoteCount,
    /// <summary>Null when results are hidden for the requesting user.</summary>
    List<string>? Voters);

public record UmfrageDto(
    Guid Id,
    string Question,
    bool IsMultipleChoice,
    bool ResultsVisible,
    bool RevealAfterClose,
    DateTime? Deadline,
    UmfrageStatus Status,
    Guid CreatedByUserId,
    string CreatedByName,
    DateTime CreatedAt,
    Guid? ClosedByUserId,
    string? ClosedByName,
    DateTime? ClosedAt,
    List<UmfrageOptionDto> Options,
    /// <summary>IDs of options the current user has selected.</summary>
    List<Guid> CurrentUserOptionIds,
    /// <summary>Total number of participants who have answered (regardless of visibility).</summary>
    int ParticipantCount);

public record CreateUmfrageDto(
    string Question,
    List<string> Options,
    bool IsMultipleChoice,
    bool ResultsVisible,
    bool RevealAfterClose,
    DateTime? Deadline);

public record UpdateUmfrageDto(
    string Question,
    List<UpdateUmfrageOptionDto> Options,
    bool IsMultipleChoice,
    bool ResultsVisible,
    bool RevealAfterClose,
    DateTime? Deadline);

public record UpdateUmfrageOptionDto(
    Guid? Id,   // null = new option
    string Text);

public record VoteUmfrageDto(
    List<Guid> OptionIds);
