using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class WerkzeugConfiguration : IEntityTypeConfiguration<Werkzeug>
{
    public void Configure(EntityTypeBuilder<Werkzeug> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(w => w.Description)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(w => w.Category)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(w => w.ImageUrl)
            .HasMaxLength(1024);

        builder.Property(w => w.Dimensions)
            .HasMaxLength(256);

        builder.HasOne(w => w.BorrowedBy)
            .WithMany()
            .HasForeignKey(w => w.BorrowedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
