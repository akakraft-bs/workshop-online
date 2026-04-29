using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class DokumentOrdnerConfiguration : IEntityTypeConfiguration<DokumentOrdner>
{
    private static readonly DateTime SeedDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<DokumentOrdner> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);

        builder.HasMany(x => x.Dokumente)
            .WithOne(d => d.Folder)
            .HasForeignKey(d => d.FolderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(
            new DokumentOrdner { Id = new Guid("a1a1a1a1-0000-0000-0000-000000000001"), Name = "Verein",      CreatedByUserId = null, CreatedAt = SeedDate },
            new DokumentOrdner { Id = new Guid("a1a1a1a1-0000-0000-0000-000000000002"), Name = "Richtlinien", CreatedByUserId = null, CreatedAt = SeedDate },
            new DokumentOrdner { Id = new Guid("a1a1a1a1-0000-0000-0000-000000000003"), Name = "Anleitungen", CreatedByUserId = null, CreatedAt = SeedDate }
        );
    }
}
