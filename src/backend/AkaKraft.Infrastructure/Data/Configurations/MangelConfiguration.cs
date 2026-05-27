using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class MangelConfiguration : IEntityTypeConfiguration<Mangel>
{
    public void Configure(EntityTypeBuilder<Mangel> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(m => m.Kategorie)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.ImageUrl)
            .HasMaxLength(2048);

        builder.Property(m => m.Note)
            .HasMaxLength(1000);

        builder.HasOne(m => m.CreatedBy)
            .WithMany()
            .HasForeignKey(m => m.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.ResolvedBy)
            .WithMany()
            .HasForeignKey(m => m.ResolvedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.Anmerkungen)
            .WithOne(a => a.Mangel)
            .HasForeignKey(a => a.MangelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MangelAnmerkungConfiguration : IEntityTypeConfiguration<MangelAnmerkung>
{
    public void Configure(EntityTypeBuilder<MangelAnmerkung> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Text)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasOne(a => a.CreatedBy)
            .WithMany()
            .HasForeignKey(a => a.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
