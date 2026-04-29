using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class DokumentConfiguration : IEntityTypeConfiguration<Dokument>
{
    public void Configure(EntityTypeBuilder<Dokument> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(512);
        builder.Property(x => x.FileUrl).IsRequired().HasMaxLength(2048);
    }
}
