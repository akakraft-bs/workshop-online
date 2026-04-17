using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class FcmTokenConfiguration : IEntityTypeConfiguration<FcmToken>
{
    public void Configure(EntityTypeBuilder<FcmToken> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(4096);

        builder.HasIndex(t => t.Token)
            .IsUnique();

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
