using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBMS.Domain.Entities;

namespace RBMS.Infrastructure.Persistence.Configurations;

public class WhatsAppMessageConfiguration : IEntityTypeConfiguration<WhatsAppMessage>
{
    public void Configure(EntityTypeBuilder<WhatsAppMessage> e)
    {
        e.ToTable("whatsapp_messages");
        e.Property(x => x.ToPhone).HasMaxLength(20).IsRequired();
        e.Property(x => x.RecipientName).HasMaxLength(200);
        e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30);
        e.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.Provider).HasMaxLength(30).IsRequired();
        e.Property(x => x.ProviderMessageId).HasMaxLength(100);
        e.Property(x => x.Error).HasMaxLength(1000);
        e.Property(x => x.RelatedEntityType).HasMaxLength(40);
        e.HasIndex(x => new { x.TenantId, x.CreatedAt });
    }
}
