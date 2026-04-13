namespace AkaKraft.Infrastructure.Options;

public class MinioOptions
{
    public const string SectionName = "Minio";

    public string Endpoint { get; set; } = "";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string BucketName { get; set; } = "";
    public bool UseSSL { get; set; }

    /// <summary>
    /// Öffentliche Basis-URL für den Browser (kann sich von Endpoint unterscheiden,
    /// z. B. wenn der Backend-Container intern "minio:9000" verwendet,
    /// der Browser aber "http://localhost:9000" benötigt).
    /// </summary>
    public string PublicBaseUrl { get; set; } = "";
}
