using AkaKraft.Domain.Common;
using AkaKraft.Domain.Enums;

namespace AkaKraft.Domain.Entities;

public class Motd : IAuditable
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public MotdSeverity Severity { get; set; } = MotdSeverity.Info;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
