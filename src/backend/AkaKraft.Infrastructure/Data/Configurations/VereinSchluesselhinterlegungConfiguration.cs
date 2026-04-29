using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class VereinSchluesselhinterlegungConfiguration : IEntityTypeConfiguration<VereinSchluesselhinterlegung>
{
    public void Configure(EntityTypeBuilder<VereinSchluesselhinterlegung> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Address).IsRequired().HasMaxLength(512);
        builder.Property(x => x.Phone).HasMaxLength(64);
    }
}
