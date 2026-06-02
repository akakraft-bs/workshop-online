using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class AblageortConfiguration : IEntityTypeConfiguration<Ablageort>
{
    public void Configure(EntityTypeBuilder<Ablageort> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(256);
        builder.HasIndex(a => a.Name).IsUnique();
        builder.Property(a => a.Color).HasMaxLength(20);
    }
}
