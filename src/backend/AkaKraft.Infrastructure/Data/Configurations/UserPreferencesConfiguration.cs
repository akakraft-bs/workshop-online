using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.HasKey(p => p.UserId);

        builder.Property(p => p.FavoriteRoutesJson)
            .IsRequired()
            .HasDefaultValue("[]");

        builder.Property(p => p.DisplayName)
            .HasMaxLength(64);
    }
}
