using AkaKraft.Application.Interfaces;
using AkaKraft.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace AkaKraft.Infrastructure.Services;

public class UploadService(IMinioClient minio, IOptions<MinioOptions> opts) : IUploadService
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

        var objectName = $"werkzeug/{Guid.NewGuid()}{ext}";
        var bucket = opts.Value.BucketName;

        await minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithStreamData(file.Content)
            .WithObjectSize(file.Length)
            .WithContentType(file.ContentType));

        return $"{opts.Value.PublicBaseUrl}/{bucket}/{objectName}";
    }

    public async Task DeleteAsync(string? url)
    {
        if (string.IsNullOrEmpty(url)) return;

        var bucket = opts.Value.BucketName;
        var prefix = $"{opts.Value.PublicBaseUrl}/{bucket}/";

        // Nur MinIO-eigene URLs löschen
        if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return;

        var objectName = url[prefix.Length..];

        try
        {
            await minio.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName));
        }
        catch
        {
            // Fehler beim Löschen ignorieren (Objekt evtl. schon weg)
        }
    }
}
