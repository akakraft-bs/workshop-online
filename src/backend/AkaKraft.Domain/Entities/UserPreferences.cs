namespace AkaKraft.Domain.Entities;

public class UserPreferences
{
    public Guid UserId { get; set; }

    /// <summary>
    /// JSON-serialized list of favorite route strings, e.g. ["/werkzeug", "/kalender"]
    /// </summary>
    public string FavoriteRoutesJson { get; set; } = "[]";

    /// <summary>
    /// Optionaler Anzeigename, der z. B. als Termin-Präfix verwendet wird.
    /// </summary>
    public string? DisplayName { get; set; }
}
