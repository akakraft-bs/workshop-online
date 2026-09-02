using AkaKraft.Domain.Enums;

namespace AkaKraft.Domain.Entities;

public class CalendarConfig
{
    public Guid Id { get; set; }
    public string GoogleCalendarId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#1976D2";
    public bool IsVisible { get; set; } = true;
    public int SortOrder { get; set; }
    public CalendarType CalendarType { get; set; } = CalendarType.Hallenbelegung;

    /// <summary>
    /// Wenn true, berechtigt eine Reservierung in diesem Kalender (z. B. "Bühne Halle 1")
    /// zur Nutzung eines Parkkontos.
    /// </summary>
    public bool GrantsParkplatzBerechtigung { get; set; }

    public ICollection<CalendarWriteRole> WriteRoles { get; set; } = new List<CalendarWriteRole>();
}
