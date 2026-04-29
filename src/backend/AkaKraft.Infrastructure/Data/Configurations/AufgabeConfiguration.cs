using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class AufgabeConfiguration : IEntityTypeConfiguration<Aufgabe>
{
    public void Configure(EntityTypeBuilder<Aufgabe> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Titel).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Beschreibung).IsRequired();
        builder.Property(a => a.FotoUrl).HasMaxLength(2048);
        builder.Property(a => a.Status).IsRequired().HasMaxLength(20);
        builder.Property(a => a.AssignedName).HasMaxLength(200);

        builder.HasOne(a => a.CreatedBy)
            .WithMany()
            .HasForeignKey(a => a.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AssignedUser)
            .WithMany()
            .HasForeignKey(a => a.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
