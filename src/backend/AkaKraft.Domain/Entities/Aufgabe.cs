namespace AkaKraft.Domain.Entities;

public class Aufgabe
{
    public Guid Id { get; set; }
    public string Titel { get; set; } = string.Empty;
    public string Beschreibung { get; set; } = string.Empty;
    public string? FotoUrl { get; set; }

    /// <summary>"Neu" | "Zugewiesen" | "Erledigt"</summary>
    public string Status { get; set; } = "Neu";

    public Guid? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }

    /// <summary>Freitext für Aufnahmeaufgaben (Person ist noch kein Mitglied).</summary>
    public string? AssignedName { get; set; }

    public Guid CreatedByUserId { get; set; }
    public User CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
