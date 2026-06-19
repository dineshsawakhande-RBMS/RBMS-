using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Attendance;

public record AttendanceDto(
    Guid Id, Guid EmployeeId, DateOnly WorkDate, AttendanceStatus Status,
    TimeOnly? CheckIn, TimeOnly? CheckOut, decimal? WorkedHours, string? Remarks);

/// <summary>Monthly roll-up used to prefill payroll's working/present days.</summary>
public record AttendanceSummaryDto(
    Guid EmployeeId, int Year, int Month,
    decimal WorkingDays, decimal PresentDays,
    int Present, int Absent, int HalfDay, int Leave, int Holiday, int WeekOff);

public record LeaveRequestDto(
    Guid Id, Guid EmployeeId, string EmployeeName, LeaveType LeaveType,
    DateOnly FromDate, DateOnly ToDate, decimal Days, string? Reason,
    LeaveStatus Status, DateTimeOffset? ApprovedAt, string? DecisionNotes);
