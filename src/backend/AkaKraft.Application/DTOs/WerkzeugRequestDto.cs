namespace AkaKraft.Application.DTOs;

public record CreateWerkzeugDto(
    string Name,
    string Description,
    string Category,
    string? ImageUrl,
    string? Dimensions,
    string? StorageLocation
);

public record UpdateWerkzeugDto(
    string Name,
    string Description,
    string Category,
    string? ImageUrl,
    string? Dimensions,
    string? StorageLocation
);

public record AusleihenRequestDto(DateTime ExpectedReturnAt);
