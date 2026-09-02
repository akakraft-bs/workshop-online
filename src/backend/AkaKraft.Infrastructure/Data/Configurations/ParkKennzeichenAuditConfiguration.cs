using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class ParkKennzeichenAuditConfiguration : IEntityTypeConfiguration<ParkKennzeichenAudit>
{
    public void Configure(EntityTypeBuilder<ParkKennzeichenAudit> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AusgefuehrtVon).IsRequired().HasMaxLength(160);
        builder.Property(a => a.Kennzeichen).IsRequired().HasMaxLength(16);
        builder.Property(a => a.KennzeichenNachher).HasMaxLength(400);
        builder.Property(a => a.Aktion).HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(a => new { a.ParkAccountId, a.CreatedAt });

        builder.HasOne(a => a.ParkAccount)
            .WithMany()
            .HasForeignKey(a => a.ParkAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
