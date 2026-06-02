using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class KontakteintragConfiguration : IEntityTypeConfiguration<Kontakteintrag>
{
    public void Configure(EntityTypeBuilder<Kontakteintrag> builder)
    {
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Kanal).HasConversion<string>();
        builder.Property(k => k.Reaktion).HasConversion<string>();
        builder.Property(k => k.Zusammenfassung).IsRequired().HasMaxLength(2000);
        builder.Property(k => k.NaechsteSchritte).HasMaxLength(2000);

        builder.HasOne(k => k.Ansprechpartner)
            .WithMany()
            .HasForeignKey(k => k.AnsprechpartnerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
