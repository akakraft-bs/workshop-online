using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class ParkAccountConfiguration : IEntityTypeConfiguration<ParkAccount>
{
    public static readonly Guid KontoAId = Guid.Parse("a1a1a1a1-0000-0000-0000-0000000000a1");
    public static readonly Guid KontoBId = Guid.Parse("b2b2b2b2-0000-0000-0000-0000000000b2");

    public void Configure(EntityTypeBuilder<ParkAccount> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Label).IsRequired().HasMaxLength(80);
        builder.Property(a => a.PortalUrl).HasMaxLength(512);
        builder.Property(a => a.Notiz).HasMaxLength(1000);

        builder.HasMany(a => a.Claims)
            .WithOne(c => c.ParkAccount)
            .HasForeignKey(c => c.ParkAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // Die beiden von der Uni bereitgestellten Zugänge.
        builder.HasData(
            new ParkAccount { Id = KontoAId, Label = "Parkkonto A", SortOrder = 0 },
            new ParkAccount { Id = KontoBId, Label = "Parkkonto B", SortOrder = 1 }
        );
    }
}
