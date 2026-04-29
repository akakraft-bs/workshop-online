namespace AkaKraft.Application.DTOs;

public record ProjektDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime PlannedStartDate,
    int DurationWeeks,
    DateTime? ActualStartDate,
    DateTime? ActualEndDate,
    DateTime ExpectedEndDate,
    string Status,
    string? ProjektplanUrl,
    string CreatedByName,
    DateTime CreatedAt
);
