namespace AkaKraft.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    /// <summary>Nur bei Google-OAuth-Nutzern gesetzt.</summary>
    public string? GoogleId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    // E-Mail-/Passwort-Authentifizierung
    public string? PasswordHash { get; set; }
    public DateTime? EmailConfirmedAt { get; set; }
    public string? EmailConfirmationToken { get; set; }
    public DateTime? EmailConfirmationTokenExpiry { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
}
