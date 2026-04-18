using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class WunschVoteConfiguration : IEntityTypeConfiguration<WunschVote>
{
    public void Configure(EntityTypeBuilder<WunschVote> builder)
    {
        builder.HasKey(v => v.Id);

        // Each user can only vote once per Wunsch
        builder.HasIndex(v => new { v.WunschId, v.UserId }).IsUnique();

        builder.HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
