using AkaKraft.Domain.Common;

namespace AkaKraft.Domain.Entities;

/// <summary>
/// Position der Halle für den Standort-Check-in der Mobile-Apps (Geofence).
/// Es gibt höchstens einen Eintrag; ohne Eintrag ist die Funktion aus.
/// </summary>
public class HallenStandort : IAuditable
{
    public Guid Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
