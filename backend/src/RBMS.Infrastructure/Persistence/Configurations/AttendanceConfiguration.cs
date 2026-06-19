using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBMS.Domain.Entities;

namespace RBMS.Infrastructure.Persistence.Configurations;

public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> e)
    {
        e.ToTable("attendance");
        e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.WorkedHours).HasPrecision(5, 2);
        e.Property(x => x.Remarks).HasMaxLength(500);
        e.HasIndex(x => new { x.TenantId, x.EmployeeId, x.WorkDate }).IsUnique();
        e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
    }
}
