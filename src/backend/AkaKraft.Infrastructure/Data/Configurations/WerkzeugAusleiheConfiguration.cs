using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class WerkzeugAusleiheConfiguration : IEntityTypeConfiguration<WerkzeugAusleihe>
{
    public void Configure(EntityTypeBuilder<WerkzeugAusleihe> builder)
    {
        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.Werkzeug)
            .WithMany(w => w.Ausleihen)
            .HasForeignKey(a => a.WerkzeugId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.WerkzeugId, a.BorrowedAt });
    }
}
