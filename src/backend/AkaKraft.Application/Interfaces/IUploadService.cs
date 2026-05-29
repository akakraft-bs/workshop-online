namespace AkaKraft.Application.Interfaces;

public record FileUploadModel(Stream Content, string FileName, string ContentType, long Length);

public record UploadResult(string ImageUrl, string ThumbnailUrl);

public interface IUploadService
{
    Task<UploadResult> SaveAsync(FileUploadModel file);
    Task<string> SaveDocumentAsync(FileUploadModel file);
    Task DeleteAsync(string? url);
    Task DeleteAsync(string? imageUrl, string? thumbnailUrl);
    Task<string?> GenerateThumbnailForExistingAsync(string imageUrl);
}
