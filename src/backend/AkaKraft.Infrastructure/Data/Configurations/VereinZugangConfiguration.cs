using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class VereinZugangConfiguration : IEntityTypeConfiguration<VereinZugang>
{
    public void Configure(EntityTypeBuilder<VereinZugang> builder)
    {
        builder.HasKey(z => z.Id);
        builder.Property(z => z.Anbieter).IsRequired().HasMaxLength(256);
        builder.Property(z => z.Zugangsdaten).IsRequired();
    }
}
