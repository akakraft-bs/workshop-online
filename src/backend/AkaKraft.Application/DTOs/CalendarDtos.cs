namespace AkaKraft.Application.DTOs;

public record AvailableCalendarDto(
    string GoogleCalendarId,
    string Name,
    string? Description,
    CalendarConfigDto? Config
);

public record CalendarConfigDto(
    Guid Id,
    string GoogleCalendarId,
    string Name,
    string Color,
    bool IsVisible,
    int SortOrder,
    string CalendarType,
    IEnumerable<string> WriteRoles
);

public record UpdateCalendarConfigDto(
    string Name,
    string Color,
    bool IsVisible,
    int SortOrder,
    string CalendarType,
    IEnumerable<string> WriteRoles
);

public record SubscribeCalendarDto(string CalendarId);

public record CalendarEventDto(
    string Id,
    string CalendarId,
    string CalendarName,
    string CalendarColor,
    string Title,
    DateTime? Start,
    DateTime? End,
    bool IsAllDay,
    string? CreatorName,
    string? CreatorEmail,
    string? Description,
    string? Location
);

public record CreateCalendarEventDto(
    string CalendarId,
    string Title,
    DateTime Start,
    DateTime End,
    bool IsAllDay,
    string? Description,
    string? Location
);

public record UpdateCalendarEventDto(
    string Title,
    DateTime Start,
    DateTime End,
    bool IsAllDay,
    string? Description,
    string? Location
);
