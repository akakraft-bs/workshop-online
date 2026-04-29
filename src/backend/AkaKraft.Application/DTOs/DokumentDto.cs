namespace AkaKraft.Application.DTOs;

public record DokumentDto(
    Guid Id,
    Guid FolderId,
    string FileName,
    string FileUrl,
    string UploadedByName,
    DateTime UploadedAt,
    long? FileSizeBytes
);

public record DokumentOrdnerDto(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    IEnumerable<DokumentDto> Dokumente
);
