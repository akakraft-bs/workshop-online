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

    public string? Phone { get; set; }
    public string? Address { get; set; }

    /// <summary>
    /// Benachrichtigung wenn ein ausgeliehenes Werkzeug überfällig ist.
    /// </summary>
    public bool NotifyLeihruckgabe { get; set; } = true;

    /// <summary>
    /// Benachrichtigung bei neuen Veranstaltungen und einen Tag vorher.
    /// </summary>
    public bool NotifyVeranstaltungen { get; set; } = true;

    /// <summary>
    /// Benachrichtigung wenn ein Verbrauchsmittel den Mindestbestand unterschreitet.
    /// </summary>
    public bool NotifyMindestbestand { get; set; } = true;

    /// <summary>
    /// Benachrichtigung bei neuen Umfragen und eine Stunde vor Fristablauf.
    /// </summary>
    public bool NotifyUmfragen { get; set; } = true;
}
