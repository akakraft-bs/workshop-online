using AkaKraft.Application.Interfaces;
using AkaKraft.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace AkaKraft.Infrastructure.Services;

public class UploadService(IMinioClient minio, IOptions<MinioOptions> opts) : IUploadService
{
    private static readonly HashSet<string> AllowedImageTypes =
        ["image/jpeg", "image/png", "image/webp", "image/gif"];

    private static readonly HashSet<string> AllowedDocumentTypes =
    [
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "text/plain",
        "image/jpeg", "image/png", "image/webp", "image/gif",
    ];

    private const long MaxImageBytes    = 5  * 1024 * 1024; //  5 MB
    private const long MaxDocumentBytes = 50 * 1024 * 1024; // 50 MB
    private const int  ThumbnailSize    = 400;               // px (längste Seite)

    public async Task<UploadResult> SaveAsync(FileUploadModel file)
    {
        if (!AllowedImageTypes.Contains(file.ContentType))
            throw new InvalidOperationException("Ungültiger Dateityp. Erlaubt: JPEG, PNG, WebP, GIF.");

        if (file.Length > MaxImageBytes)
            throw new InvalidOperationException("Datei zu groß. Maximal 5 MB erlaubt.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";

        var id     = Guid.NewGuid().ToString();
        var bucket = opts.Value.BucketName;

        // Original speichern
        var originalName = $"werkzeug/{id}{ext}";
        await minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(originalName)
            .WithStreamData(file.Content)
            .WithObjectSize(file.Length)
            .WithContentType(file.ContentType));

        // Thumbnail generieren (Stream zurückspulen)
        file.Content.Seek(0, SeekOrigin.Begin);
        var thumbnailName = $"werkzeug/thumb_{id}.jpg";
        await using var thumbStream = await GenerateThumbnailAsync(file.Content);
        await minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(thumbnailName)
            .WithStreamData(thumbStream)
            .WithObjectSize(thumbStream.Length)
            .WithContentType("image/jpeg"));

        var baseUrl  = opts.Value.PublicBaseUrl;
        return new UploadResult(
            ImageUrl:     $"{baseUrl}/{bucket}/{originalName}",
            ThumbnailUrl: $"{baseUrl}/{bucket}/{thumbnailName}"
        );
    }

    public async Task<string> SaveDocumentAsync(FileUploadModel file)
    {
        if (!AllowedDocumentTypes.Contains(file.ContentType))
            throw new InvalidOperationException(
                "Ungültiger Dateityp. Erlaubt: PDF, Word, Excel, PowerPoint, Text, Bilder.");

        if (file.Length > MaxDocumentBytes)
            throw new InvalidOperationException("Datei zu groß. Maximal 50 MB erlaubt.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) ext = ".bin";

        var objectName = $"dokumente/{Guid.NewGuid()}{ext}";
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
        await DeleteObjectAsync(url);
    }

    public async Task DeleteAsync(string? imageUrl, string? thumbnailUrl)
    {
        await Task.WhenAll(DeleteAsync(imageUrl), DeleteAsync(thumbnailUrl));
    }

    public async Task<string?> GenerateThumbnailForExistingAsync(string imageUrl)
    {
        var bucket = opts.Value.BucketName;
        var prefix = $"{opts.Value.PublicBaseUrl}/{bucket}/";

        if (!imageUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var objectName = imageUrl[prefix.Length..];

        // Original aus MinIO laden
        using var original = new MemoryStream();
        try
        {
            await minio.GetObjectAsync(new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithCallbackStream((stream, _) => stream.CopyToAsync(original)));
        }
        catch
        {
            return null; // Objekt nicht gefunden oder Fehler
        }

        original.Seek(0, SeekOrigin.Begin);

        // Thumbnail-Objektname ableiten: werkzeug/abc.jpg → werkzeug/thumb_abc.jpg
        var dir  = Path.GetDirectoryName(objectName)?.Replace('\\', '/') ?? string.Empty;
        var file = Path.GetFileNameWithoutExtension(objectName);
        var thumbnailName = string.IsNullOrEmpty(dir)
            ? $"thumb_{file}.jpg"
            : $"{dir}/thumb_{file}.jpg";

        await using var thumbStream = await GenerateThumbnailAsync(original);
        await minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(thumbnailName)
            .WithStreamData(thumbStream)
            .WithObjectSize(thumbStream.Length)
            .WithContentType("image/jpeg"));

        return $"{opts.Value.PublicBaseUrl}/{bucket}/{thumbnailName}";
    }

    private async Task DeleteObjectAsync(string url)
    {
        var bucket = opts.Value.BucketName;
        var prefix = $"{opts.Value.PublicBaseUrl}/{bucket}/";

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

    private static async Task<MemoryStream> GenerateThumbnailAsync(Stream input)
    {
        using var image = await Image.LoadAsync(input);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(ThumbnailSize, ThumbnailSize),
            Mode = ResizeMode.Max,
        }));

        var output = new MemoryStream();
        await image.SaveAsync(output, new JpegEncoder { Quality = 82 });
        output.Seek(0, SeekOrigin.Begin);
        return output;
    }
}
