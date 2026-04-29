namespace AkaKraft.Domain.Entities;

public class Dokument
{
    public Guid Id { get; set; }
    public Guid FolderId { get; set; }
    public DokumentOrdner Folder { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public Guid UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; }
    public long? FileSizeBytes { get; set; }
}
