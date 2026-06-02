using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(256);
        builder.Property(p => p.Kategorie).HasMaxLength(128);
        builder.Property(p => p.Status).HasConversion<string>();
        builder.Property(p => p.Website).HasMaxLength(512);
        builder.Property(p => p.Notizen).HasMaxLength(4000);

        builder.HasMany(p => p.Ansprechpartner)
            .WithOne(a => a.Partner)
            .HasForeignKey(a => a.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Kontakteintraege)
            .WithOne(k => k.Partner)
            .HasForeignKey(k => k.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
