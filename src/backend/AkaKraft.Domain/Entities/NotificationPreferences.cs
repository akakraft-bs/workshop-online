namespace AkaKraft.Domain.Entities;

public class NotificationPreferences
{
    public Guid UserId { get; set; }
    public bool WerkzeugRueckgabe { get; set; } = true;
    public bool Veranstaltungen { get; set; } = true;
    public bool VerbrauchsmaterialMindestbestand { get; set; } = false;
}
