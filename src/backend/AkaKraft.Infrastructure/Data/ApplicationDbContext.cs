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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
        modelBuilder.ApplyConfiguration(new WerkzeugConfiguration());
        modelBuilder.ApplyConfiguration(new VerbrauchsmaterialConfiguration());
    }
}
