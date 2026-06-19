using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBMS.Domain.Entities;

namespace RBMS.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> e)
    {
        e.ToTable("customers");
        e.Property(x => x.Name).HasMaxLength(300).IsRequired();
        e.Property(x => x.Mobile).HasMaxLength(20).IsRequired();
        e.Property(x => x.Email).HasMaxLength(256);
        e.HasIndex(x => new { x.TenantId, x.Mobile }).IsUnique();
    }
}

public class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransaction> e)
    {
        e.ToTable("loyalty_transactions");
        e.Property(x => x.TxnType).HasConversion<string>().HasMaxLength(20);
        e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
        e.HasIndex(x => x.CustomerId);
    }
}
