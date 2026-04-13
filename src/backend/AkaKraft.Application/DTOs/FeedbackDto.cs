using AkaKraft.Domain.Enums;

namespace AkaKraft.Application.DTOs;

public record FeedbackDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string Text,
    string PageUrl,
    FeedbackStatus Status,
    DateTime CreatedAt
);

public record CreateFeedbackDto(string Text, string PageUrl);

public record UpdateFeedbackStatusDto(FeedbackStatus Status);
