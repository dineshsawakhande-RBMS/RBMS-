using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBMS.Domain.Entities;

namespace RBMS.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> e)
    {
        e.ToTable("documents");
        e.Property(x => x.Title).HasMaxLength(300).IsRequired();
        e.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(40);
        e.Property(x => x.Description).HasMaxLength(2000);
        e.Property(x => x.Tags).HasMaxLength(500);
        e.Property(x => x.FileKey).HasMaxLength(500).IsRequired();
        e.Property(x => x.FileName).HasMaxLength(300).IsRequired();
        e.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
        e.Property(x => x.RelatedEntityType).HasMaxLength(40);
        e.HasIndex(x => new { x.TenantId, x.DocumentType });
        e.HasIndex(x => new { x.TenantId, x.ExpiryDate });
    }
}
