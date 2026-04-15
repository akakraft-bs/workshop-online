using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class CalendarConfigConfiguration : IEntityTypeConfiguration<CalendarConfig>
{
    public void Configure(EntityTypeBuilder<CalendarConfig> builder)
    {
        builder.HasKey(c => c.Id);

        builder.HasIndex(c => c.GoogleCalendarId)
            .IsUnique();

        builder.Property(c => c.GoogleCalendarId)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.Color)
            .IsRequired()
            .HasMaxLength(32);

        builder.HasMany(c => c.WriteRoles)
            .WithOne(r => r.CalendarConfig)
            .HasForeignKey(r => r.CalendarConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
