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
        builder.Property(k => k.Zusammenfassung).IsRequired();
        builder.Property(k => k.NaechsteSchritte);

        builder.HasOne(k => k.Ansprechpartner)
            .WithMany()
            .HasForeignKey(k => k.AnsprechpartnerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
