namespace AkaKraft.Application.DTOs;

public record CreateWerkzeugDto(
    string Name,
    string Description,
    string Category,
    string? ImageUrl,
    string? Dimensions
);

public record UpdateWerkzeugDto(
    string Name,
    string Description,
    string Category,
    string? ImageUrl,
    string? Dimensions
);

public record AusleihenRequestDto(DateTime ExpectedReturnAt);
