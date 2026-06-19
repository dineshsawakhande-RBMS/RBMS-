using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBMS.Domain.Entities;

namespace RBMS.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> e)
    {
        e.ToTable("employees");
        e.Property(x => x.EmployeeCode).HasMaxLength(20).IsRequired();
        e.Property(x => x.FullName).HasMaxLength(300).IsRequired();
        e.Property(x => x.Mobile).HasMaxLength(20).IsRequired();
        e.Property(x => x.Email).HasMaxLength(256);
        e.Property(x => x.Gender).HasMaxLength(10);
        e.Property(x => x.Ifsc).HasMaxLength(11);
        e.Property(x => x.AccountLast4).HasMaxLength(4);
        e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.MonthlyCtc).HasPrecision(14, 2);
        e.HasIndex(x => new { x.TenantId, x.EmployeeCode }).IsUnique();
    }
}
