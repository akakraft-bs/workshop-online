namespace AkaKraft.Application;

/// <summary>Angaben zur laufenden Backend-Instanz.</summary>
public static class AppInfo
{
    /// <summary>Release-Version des Backends (Docker-Build-Arg <c>BUILD_VERSION</c> → <c>APP_VERSION</c>), lokal „dev“.</summary>
    public static string Version => Environment.GetEnvironmentVariable("APP_VERSION") is { Length: > 0 } v ? v : "dev";
}
