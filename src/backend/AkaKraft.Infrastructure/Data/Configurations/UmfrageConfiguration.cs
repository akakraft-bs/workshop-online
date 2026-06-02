using AkaKraft.Domain.Entities;
using AkaKraft.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class UmfrageConfiguration : IEntityTypeConfiguration<Umfrage>
{
    public void Configure(EntityTypeBuilder<Umfrage> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Question)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(u => u.LinkedEventId).HasMaxLength(256);
        builder.Property(u => u.LinkedCalendarId).HasMaxLength(256);
        builder.Property(u => u.LinkedEventTitle).HasMaxLength(500);
        builder.Property(u => u.Description).HasMaxLength(2000);

        builder.HasOne(u => u.CreatedBy)
            .WithMany()
            .HasForeignKey(u => u.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.ClosedBy)
            .WithMany()
            .HasForeignKey(u => u.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Options)
            .WithOne(o => o.Umfrage)
            .HasForeignKey(o => o.UmfrageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Antworten)
            .WithOne(a => a.Umfrage)
            .HasForeignKey(a => a.UmfrageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Enthaltungen)
            .WithOne(e => e.Umfrage)
            .HasForeignKey(e => e.UmfrageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UmfrageEnthaltungConfiguration : IEntityTypeConfiguration<UmfrageEnthaltung>
{
    public void Configure(EntityTypeBuilder<UmfrageEnthaltung> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.UmfrageId, e.UserId }).IsUnique();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UmfrageOptionConfiguration : IEntityTypeConfiguration<UmfrageOption>
{
    public void Configure(EntityTypeBuilder<UmfrageOption> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Text)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasMany(o => o.Antworten)
            .WithOne(a => a.Option)
            .HasForeignKey(a => a.OptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UmfrageAntwortConfiguration : IEntityTypeConfiguration<UmfrageAntwort>
{
    public void Configure(EntityTypeBuilder<UmfrageAntwort> builder)
    {
        builder.HasKey(a => a.Id);

        // One answer per user per option
        builder.HasIndex(a => new { a.UmfrageId, a.OptionId, a.UserId }).IsUnique();

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
