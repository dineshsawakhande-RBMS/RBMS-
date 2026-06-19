using RBMS.Domain.Common;
using RBMS.Domain.Enums;

namespace RBMS.Domain.Entities;

/// <summary>An employee leave request with an approval workflow. Maps to the "leaves" table.</summary>
public class LeaveRequest : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public LeaveType LeaveType { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal Days { get; set; }
    public string? Reason { get; set; }
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public Guid? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? DecisionNotes { get; set; }

    public Employee Employee { get; set; } = null!;
}
