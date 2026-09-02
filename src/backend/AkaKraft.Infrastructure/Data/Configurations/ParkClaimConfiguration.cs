using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class ParkClaimConfiguration : IEntityTypeConfiguration<ParkClaim>
{
    public void Configure(EntityTypeBuilder<ParkClaim> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Kennzeichen).IsRequired().HasMaxLength(16);
        builder.Property(c => c.FahrzeugBezeichnung).HasMaxLength(160);
        builder.Property(c => c.BestaetigungHinweis).HasMaxLength(500);
        builder.Property(c => c.BookingEventId).HasMaxLength(256);
        builder.Property(c => c.BerechtigungArt).HasConversion<string>().HasMaxLength(32);

        // Für die "ist dieses Konto frei?"-Abfrage.
        builder.HasIndex(c => new { c.ParkAccountId, c.FreigegebenAt });
        builder.HasIndex(c => c.UserId);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
