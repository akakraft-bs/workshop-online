using AkaKraft.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace AkaKraft.Infrastructure.Services;

public class UploadService(IWebHostEnvironment env) : IUploadService
{
    private static readonly HashSet<string> AllowedTypes =
        ["image/jpeg", "image/png", "image/webp", "image/gif"];

    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    public async Task<string> SaveAsync(FileUploadModel file)
    {
        if (!AllowedTypes.Contains(file.ContentType))
            throw new InvalidOperationException("Ungültiger Dateityp. Erlaubt: JPEG, PNG, WebP, GIF.");

        if (file.Length > MaxBytes)
            throw new InvalidOperationException("Datei zu groß. Maximal 5 MB erlaubt.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";

        var fileName = $"{Guid.NewGuid()}{ext}";
        var folder = Path.Combine(env.WebRootPath, "uploads", "werkzeug");
        Directory.CreateDirectory(folder);

        var filePath = Path.Combine(folder, fileName);
        await using var stream = File.Create(filePath);
        await file.Content.CopyToAsync(stream);

        return $"/uploads/werkzeug/{fileName}";
    }

    public void DeleteIfLocal(string? url)
    {
        if (string.IsNullOrEmpty(url) || !url.StartsWith("/uploads/"))
            return;

        var relativePath = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(env.WebRootPath, relativePath);

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
