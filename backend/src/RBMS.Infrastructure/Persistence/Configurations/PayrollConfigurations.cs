using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBMS.Domain.Entities;

namespace RBMS.Infrastructure.Persistence.Configurations;

public class PayrollConfiguration : IEntityTypeConfiguration<Payroll>
{
    public void Configure(EntityTypeBuilder<Payroll> e)
    {
        e.ToTable("payrolls");
        e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        foreach (var p in new[]
                 {
                     nameof(Payroll.WorkingDays), nameof(Payroll.PresentDays), nameof(Payroll.GrossEarnings),
                     nameof(Payroll.Bonus), nameof(Payroll.TotalDeductions), nameof(Payroll.AdvanceDeducted),
                     nameof(Payroll.NetPay)
                 })
            e.Property(p).HasPrecision(14, 2);
        e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId);
        e.HasMany(x => x.Lines).WithOne(l => l.Payroll).HasForeignKey(l => l.PayrollId);
        e.HasIndex(x => new { x.EmployeeId, x.PeriodYear, x.PeriodMonth }).IsUnique();
    }
}

public class PayrollLineConfiguration : IEntityTypeConfiguration<PayrollLine>
{
    public void Configure(EntityTypeBuilder<PayrollLine> e)
    {
        e.ToTable("payroll_lines");
        e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.Amount).HasPrecision(14, 2);
    }
}

public class SalaryAdvanceConfiguration : IEntityTypeConfiguration<SalaryAdvance>
{
    public void Configure(EntityTypeBuilder<SalaryAdvance> e)
    {
        e.ToTable("salary_advances");
        e.Property(x => x.Amount).HasPrecision(14, 2);
        e.Property(x => x.Recovered).HasPrecision(14, 2);
        e.Ignore(x => x.Outstanding);
        e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId);
        e.HasIndex(x => x.EmployeeId);
    }
}
