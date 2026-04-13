using AkaKraft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkaKraft.Infrastructure.Data.Configurations;

public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Text)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(f => f.PageUrl)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(f => f.Status)
            .IsRequired()
            .HasMaxLength(32)
            .HasConversion<string>();

        builder.HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
