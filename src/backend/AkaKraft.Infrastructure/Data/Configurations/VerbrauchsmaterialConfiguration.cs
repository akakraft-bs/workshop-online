using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class VerbrauchsmaterialConfiguration : IEntityTypeConfiguration<Verbrauchsmaterial>
{
    public void Configure(EntityTypeBuilder<Verbrauchsmaterial> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(v => v.Description)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(v => v.Category)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(v => v.Unit)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(v => v.ImageUrl)
            .HasMaxLength(1024);
    }
}
