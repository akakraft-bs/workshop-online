using AkaKraft.Domain.Enums;

namespace AkaKraft.Application.DTOs;

public record FeedbackDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string Text,
    string PageUrl,
    string? AppVersion,
    FeedbackStatus Status,
    DateTime CreatedAt
);

public record CreateFeedbackDto(string Text, string PageUrl, string? AppVersion);

public record UpdateFeedbackStatusDto(FeedbackStatus Status);
