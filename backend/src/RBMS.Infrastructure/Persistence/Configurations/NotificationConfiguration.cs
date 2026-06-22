using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBMS.Domain.Entities;

namespace RBMS.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> e)
    {
        e.ToTable("notifications");
        e.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
        e.Property(x => x.Severity).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.Title).HasMaxLength(200).IsRequired();
        e.Property(x => x.Message).HasMaxLength(1000).IsRequired();
        e.Property(x => x.LinkPath).HasMaxLength(200);
        e.Property(x => x.DedupKey).HasMaxLength(200).IsRequired();
        e.Property(x => x.RelatedEntityType).HasMaxLength(40);
        // Filtered so a soft-deleted (resolved) alert doesn't block re-creating it later.
        e.HasIndex(x => new { x.TenantId, x.UserId, x.DedupKey })
            .IsUnique().HasFilter("is_deleted = false");
        e.HasIndex(x => new { x.TenantId, x.UserId, x.IsRead });
    }
}
