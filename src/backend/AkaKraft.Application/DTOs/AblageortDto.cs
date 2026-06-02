namespace AkaKraft.Application.DTOs;

public record StorageLocationDto(string Name, string? Color);

public record AblageortDto(Guid Id, string Name, string? Color);

public record AblageortOverviewDto(Guid? Id, string Name, string? Color, int ItemCount);

public record CreateAblageortDto(string Name, string? Color);

/// <summary>Rename a name-only (no Ablageort record) storage location.</summary>
public record RenameByNameDto(string CurrentName, string NewName, string? Color);

public record UpdateAblageortDto(string Name, string? Color);

public record MergeFromNameDto(string SourceName);
