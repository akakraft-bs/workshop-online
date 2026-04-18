using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class WunschConfiguration : IEntityTypeConfiguration<Wunsch>
{
    public void Configure(EntityTypeBuilder<Wunsch> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(w => w.Link)
            .HasMaxLength(2048);

        builder.Property(w => w.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(w => w.CloseNote)
            .HasMaxLength(1000);

        builder.HasOne(w => w.CreatedBy)
            .WithMany()
            .HasForeignKey(w => w.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.ClosedBy)
            .WithMany()
            .HasForeignKey(w => w.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(w => w.Votes)
            .WithOne(v => v.Wunsch)
            .HasForeignKey(v => v.WunschId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
