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
    public DbSet<FcmToken> FcmTokens => Set<FcmToken>();
    public DbSet<Mangel> Maengel => Set<Mangel>();
    public DbSet<Wunsch> Wuensche => Set<Wunsch>();
    public DbSet<WunschVote> WunschVotes => Set<WunschVote>();
    public DbSet<Umfrage> Umfragen => Set<Umfrage>();
    public DbSet<UmfrageOption> UmfrageOptions => Set<UmfrageOption>();
    public DbSet<UmfrageAntwort> UmfrageAntworten => Set<UmfrageAntwort>();
    public DbSet<HallenbuchEintrag> HallenbuchEintraege => Set<HallenbuchEintrag>();
    public DbSet<VereinAmtsTraegerKontakt> VereinAmtsTraegerKontakte => Set<VereinAmtsTraegerKontakt>();
    public DbSet<VereinSchluesselhinterlegung> VereinSchluesselhinterlegungen => Set<VereinSchluesselhinterlegung>();
    public DbSet<DokumentOrdner> DokumentOrdner => Set<DokumentOrdner>();
    public DbSet<Dokument> Dokumente => Set<Dokument>();
    public DbSet<Projekt> Projekte => Set<Projekt>();
    public DbSet<VereinZugang> VereinZugaenge => Set<VereinZugang>();
    public DbSet<Aufgabe> Aufgaben => Set<Aufgabe>();

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
        modelBuilder.ApplyConfiguration(new FcmTokenConfiguration());
        modelBuilder.ApplyConfiguration(new MangelConfiguration());
        modelBuilder.ApplyConfiguration(new WunschConfiguration());
        modelBuilder.ApplyConfiguration(new WunschVoteConfiguration());
        modelBuilder.ApplyConfiguration(new UmfrageConfiguration());
        modelBuilder.ApplyConfiguration(new UmfrageOptionConfiguration());
        modelBuilder.ApplyConfiguration(new UmfrageAntwortConfiguration());
        modelBuilder.ApplyConfiguration(new HallenbuchEintragConfiguration());
        modelBuilder.ApplyConfiguration(new VereinAmtsTraegerKontaktConfiguration());
        modelBuilder.ApplyConfiguration(new VereinSchluesselhinterlegungConfiguration());
        modelBuilder.ApplyConfiguration(new DokumentOrdnerConfiguration());
        modelBuilder.ApplyConfiguration(new DokumentConfiguration());
        modelBuilder.ApplyConfiguration(new ProjektConfiguration());
        modelBuilder.ApplyConfiguration(new VereinZugangConfiguration());
        modelBuilder.ApplyConfiguration(new AufgabeConfiguration());
    }
}
