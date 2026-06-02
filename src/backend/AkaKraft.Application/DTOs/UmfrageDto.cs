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
    string? Description,
    bool IsMultipleChoice,
    bool ResultsVisible,
    bool RevealAfterClose,
    DateTime? Deadline,
    DateTime? LastManualReminderSentAt,
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
    /// <summary>Whether the current user has explicitly abstained.</summary>
    bool CurrentUserAbstained,
    /// <summary>Total number of participants (voters + abstainers).</summary>
    int ParticipantCount,
    /// <summary>Number of users who explicitly abstained.</summary>
    int EnthaltungCount,
    string? LinkedEventId,
    string? LinkedCalendarId,
    string? LinkedEventTitle,
    DateTime? LinkedEventStart);

public record CreateUmfrageDto(
    string Question,
    string? Description,
    List<string> Options,
    bool IsMultipleChoice,
    bool ResultsVisible,
    bool RevealAfterClose,
    DateTime? Deadline,
    string? LinkedEventId,
    string? LinkedCalendarId,
    string? LinkedEventTitle,
    DateTime? LinkedEventStart);

public record UpdateUmfrageDto(
    string Question,
    string? Description,
    List<UpdateUmfrageOptionDto> Options,
    bool IsMultipleChoice,
    bool ResultsVisible,
    bool RevealAfterClose,
    DateTime? Deadline,
    string? LinkedEventId,
    string? LinkedCalendarId,
    string? LinkedEventTitle,
    DateTime? LinkedEventStart);

public record UpdateUmfrageOptionDto(
    Guid? Id,   // null = new option
    string Text);

public record VoteUmfrageDto(
    List<Guid> OptionIds);
