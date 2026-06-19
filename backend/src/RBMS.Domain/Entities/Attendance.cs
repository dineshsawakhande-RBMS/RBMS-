using RBMS.Domain.Common;
using RBMS.Domain.Enums;

namespace RBMS.Domain.Entities;

/// <summary>One attendance record per employee per day. Feeds payroll's present-days proration.</summary>
public class Attendance : AuditableEntity
{
    public Guid? StoreId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateOnly WorkDate { get; set; }
    public AttendanceStatus Status { get; set; }
    public TimeOnly? CheckIn { get; set; }
    public TimeOnly? CheckOut { get; set; }
    public decimal? WorkedHours { get; set; }
    public string? Remarks { get; set; }

    public Employee Employee { get; set; } = null!;
}
