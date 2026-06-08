namespace AkaKraft.Application.DTOs;

public record CreateWerkzeugDto(
    string Name,
    string Description,
    string Category,
    string? ImageUrl,
    string? ThumbnailUrl,
    string? Dimensions,
    string? StorageLocation,
    Guid? AnleitungDokumentId
);

public record UpdateWerkzeugDto(
    string Name,
    string Description,
    string Category,
    string? ImageUrl,
    string? ThumbnailUrl,
    string? Dimensions,
    string? StorageLocation,
    Guid? AnleitungDokumentId
);

public record AusleihenRequestDto(DateTime ExpectedReturnAt);
public record UpdateReturnDateDto(DateTime ExpectedReturnAt);
