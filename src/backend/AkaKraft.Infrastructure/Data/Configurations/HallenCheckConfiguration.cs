using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class HallenCheckConfiguration : IEntityTypeConfiguration<HallenCheck>
{
    public void Configure(EntityTypeBuilder<HallenCheck> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Message).HasMaxLength(200);

        builder.HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
