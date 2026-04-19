using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class HallenbuchEintragConfiguration : IEntityTypeConfiguration<HallenbuchEintrag>
{
    public void Configure(EntityTypeBuilder<HallenbuchEintrag> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Description)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(h => h.GastschraubenArt)
            .HasConversion<string>();

        builder.HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => h.UserId);
        builder.HasIndex(h => h.Start);
    }
}
