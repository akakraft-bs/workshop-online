using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Werkzeug> Werkzeuge => Set<Werkzeug>();
    public DbSet<Verbrauchsmaterial> Verbrauchsmaterialien => Set<Verbrauchsmaterial>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<CalendarConfig> CalendarConfigs => Set<CalendarConfig>();
    public DbSet<CalendarWriteRole> CalendarWriteRoles => Set<CalendarWriteRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
        modelBuilder.ApplyConfiguration(new WerkzeugConfiguration());
        modelBuilder.ApplyConfiguration(new VerbrauchsmaterialConfiguration());
        modelBuilder.ApplyConfiguration(new FeedbackConfiguration());
        modelBuilder.ApplyConfiguration(new CalendarConfigConfiguration());
        modelBuilder.ApplyConfiguration(new CalendarWriteRoleConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new UserPreferencesConfiguration());
    }
}
