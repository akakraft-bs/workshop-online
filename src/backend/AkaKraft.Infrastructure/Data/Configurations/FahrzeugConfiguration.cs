using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class FahrzeugConfiguration : IEntityTypeConfiguration<Fahrzeug>
{
    public void Configure(EntityTypeBuilder<Fahrzeug> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Marke).IsRequired().HasMaxLength(80);
        builder.Property(f => f.Modell).HasMaxLength(80);
        builder.Property(f => f.Kennzeichen).IsRequired().HasMaxLength(16);

        builder.HasIndex(f => f.UserId);

        builder.HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
