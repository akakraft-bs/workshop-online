using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class HallenStandortConfiguration : IEntityTypeConfiguration<HallenStandort>
{
    public void Configure(EntityTypeBuilder<HallenStandort> builder)
    {
        builder.HasKey(s => s.Id);
    }
}
