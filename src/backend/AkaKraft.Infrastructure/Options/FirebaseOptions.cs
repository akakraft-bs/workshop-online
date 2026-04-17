namespace AkaKraft.Infrastructure.Options;

public class FirebaseOptions
{
    public const string SectionName = "Firebase";

    /// <summary>
    /// Inhalt der Firebase Admin SDK Service Account JSON-Datei.
    /// </summary>
    public string AdminSdkJson { get; set; } = string.Empty;
}
