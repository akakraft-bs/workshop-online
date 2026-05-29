using AkaKraft.Domain.Common;
using AkaKraft.Domain.Enums;

namespace AkaKraft.Domain.Entities;

public class HallenbuchEintrag : IAuditable
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool HatGastgeschraubt { get; set; }
    public GastschraubenArt? GastschraubenArt { get; set; }
    public bool? GastschraubenBezahlt { get; set; }

    public bool HatFamiliegeschraubt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
