namespace AkaKraft.Application.DTOs;

public record CreateOrdnerDto(string Name);

public record CreateDokumentDto(Guid FolderId, string FileName, string FileUrl, long? FileSizeBytes);
