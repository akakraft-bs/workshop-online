using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class VereinAmtsTraegerKontaktConfiguration : IEntityTypeConfiguration<VereinAmtsTraegerKontakt>
{
    public void Configure(EntityTypeBuilder<VereinAmtsTraegerKontakt> builder)
    {
        builder.HasKey(x => x.UserId);

        builder.Property(x => x.Phone).HasMaxLength(64);
        builder.Property(x => x.Address).HasMaxLength(512);
    }
}
