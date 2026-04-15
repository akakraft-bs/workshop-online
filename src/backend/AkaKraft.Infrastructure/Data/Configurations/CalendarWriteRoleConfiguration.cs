using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class CalendarWriteRoleConfiguration : IEntityTypeConfiguration<CalendarWriteRole>
{
    public void Configure(EntityTypeBuilder<CalendarWriteRole> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.HasIndex(r => new { r.CalendarConfigId, r.Role })
            .IsUnique();
    }
}
