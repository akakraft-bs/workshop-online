using AkaKraft.Domain.Enums;

namespace AkaKraft.Domain.Entities;

public class CalendarWriteRole
{
    public Guid Id { get; set; }
    public Guid CalendarConfigId { get; set; }
    public CalendarConfig CalendarConfig { get; set; } = null!;
    public Role Role { get; set; }
}
