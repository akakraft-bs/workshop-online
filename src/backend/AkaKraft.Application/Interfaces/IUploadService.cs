namespace AkaKraft.Application.Interfaces;

public record FileUploadModel(Stream Content, string FileName, string ContentType, long Length);

public interface IUploadService
{
    Task<string> SaveAsync(FileUploadModel file);
    Task<string> SaveDocumentAsync(FileUploadModel file);
    Task DeleteAsync(string? url);
}
