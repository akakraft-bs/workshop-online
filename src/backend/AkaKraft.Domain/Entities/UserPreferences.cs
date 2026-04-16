namespace AkaKraft.Domain.Entities;

public class UserPreferences
{
    public Guid UserId { get; set; }

    /// <summary>
    /// JSON-serialized list of favorite route strings, e.g. ["/werkzeug", "/kalender"]
    /// </summary>
    public string FavoriteRoutesJson { get; set; } = "[]";
}
