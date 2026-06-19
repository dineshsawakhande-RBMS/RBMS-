using RBMS.Domain.Common;
using RBMS.Domain.Enums;

namespace RBMS.Domain.Entities;

/// <summary>A monthly payroll run for one employee.</summary>
public class Payroll : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }          // 1..12
    public decimal WorkingDays { get; set; }
    public decimal PresentDays { get; set; }
    public decimal GrossEarnings { get; set; }
    public decimal Bonus { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal AdvanceDeducted { get; set; }
    public decimal NetPay { get; set; }
    public PayrollStatus Status { get; set; } = PayrollStatus.Generated;
    public DateTimeOffset? PaidAt { get; set; }

    public Employee Employee { get; set; } = null!;
    public ICollection<PayrollLine> Lines { get; set; } = new List<PayrollLine>();
}

public class PayrollLine : BaseEntity
{
    public Guid PayrollId { get; set; }
    public string Name { get; set; } = null!;
    public SalaryComponentKind Kind { get; set; }
    public decimal Amount { get; set; }

    public Payroll Payroll { get; set; } = null!;
}

/// <summary>A salary advance, recovered automatically by subsequent payroll runs.</summary>
public class SalaryAdvance : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly AdvanceDate { get; set; }
    public decimal Recovered { get; set; }
    public string? Notes { get; set; }

    public Employee Employee { get; set; } = null!;

    public decimal Outstanding => Amount - Recovered;
}
