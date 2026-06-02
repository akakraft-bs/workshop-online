using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class AnsprechpartnerConfiguration : IEntityTypeConfiguration<Ansprechpartner>
{
    public void Configure(EntityTypeBuilder<Ansprechpartner> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(256);
        builder.Property(a => a.Position).HasMaxLength(128);
        builder.Property(a => a.Email).HasMaxLength(256);
        builder.Property(a => a.Telefon).HasMaxLength(64);
        builder.Property(a => a.Notizen).HasMaxLength(2000);
    }
}
