namespace AkaKraft.Application.DTOs;

public record CreateProjektDto(
    string Name,
    string? Description,
    DateTime PlannedStartDate,
    int DurationWeeks,
    DateTime? ActualStartDate,
    string Status,
    string? ProjektplanUrl
);

public record UpdateProjektDto(
    string Name,
    string? Description,
    DateTime PlannedStartDate,
    int DurationWeeks,
    DateTime? ActualStartDate,
    string Status,
    string? ProjektplanUrl
);
