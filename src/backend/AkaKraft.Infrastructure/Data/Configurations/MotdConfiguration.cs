using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class MotdConfiguration : IEntityTypeConfiguration<Motd>
{
    public void Configure(EntityTypeBuilder<Motd> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Message).IsRequired().HasMaxLength(1000);
        builder.Property(m => m.Severity).HasConversion<string>();
    }
}
