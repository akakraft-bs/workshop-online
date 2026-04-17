using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Endpoint)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(p => p.P256DH)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.Auth)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.UserId, p.Endpoint })
            .IsUnique();
    }
}
