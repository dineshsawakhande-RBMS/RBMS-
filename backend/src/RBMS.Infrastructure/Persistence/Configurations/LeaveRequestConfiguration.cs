using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBMS.Domain.Entities;

namespace RBMS.Infrastructure.Persistence.Configurations;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> e)
    {
        e.ToTable("leaves");
        e.Property(x => x.LeaveType).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.Days).HasPrecision(5, 1);
        e.Property(x => x.Reason).HasMaxLength(1000);
        e.Property(x => x.DecisionNotes).HasMaxLength(1000);
        e.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Status });
        e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
    }
}
