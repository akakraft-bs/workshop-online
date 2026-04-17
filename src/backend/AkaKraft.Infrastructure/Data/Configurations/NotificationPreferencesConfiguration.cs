using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class NotificationPreferencesConfiguration : IEntityTypeConfiguration<NotificationPreferences>
{
    public void Configure(EntityTypeBuilder<NotificationPreferences> builder)
    {
        builder.HasKey(p => p.UserId);

        builder.Property(p => p.WerkzeugRueckgabe)
            .HasDefaultValue(true);

        builder.Property(p => p.Veranstaltungen)
            .HasDefaultValue(true);

        builder.Property(p => p.VerbrauchsmaterialMindestbestand)
            .HasDefaultValue(false);
    }
}
