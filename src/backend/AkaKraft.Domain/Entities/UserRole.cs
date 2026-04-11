using AkaKraft.Domain.Enums;

namespace AkaKraft.Domain.Entities;

public class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Role Role { get; set; }
    public DateTime AssignedAt { get; set; }
}
