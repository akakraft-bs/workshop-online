namespace AkaKraft.Application.DTOs;

public record StorageLocationDto(string Name, string? Color);

public record AblageortDto(Guid Id, string Name, string? Color);

public record AblageortOverviewDto(Guid? Id, string Name, string? Color, int ItemCount);

public record CreateAblageortDto(string Name, string? Color, string? OldName = null);

public record UpdateAblageortDto(string Name, string? Color);

public record MergeFromNameDto(string SourceName);
