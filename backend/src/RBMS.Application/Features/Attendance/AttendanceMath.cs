using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Attendance;

/// <summary>Shared rules for turning daily attendance into payroll numbers.</summary>
public static class AttendanceMath
{
    /// <summary>Half a day's credit for a <see cref="AttendanceStatus.HalfDay"/>.</summary>
    public static decimal PresentCredit(AttendanceStatus status) => status switch
    {
        AttendanceStatus.Present => 1m,
        AttendanceStatus.HalfDay => 0.5m,
        _ => 0m,   // Absent / Leave (unpaid) / Holiday / WeekOff contribute no present-day
    };

    /// <summary>Holidays and weekly-offs are not working days; everything else is.</summary>
    public static bool CountsAsWorkingDay(AttendanceStatus status) =>
        status is not (AttendanceStatus.Holiday or AttendanceStatus.WeekOff);
}
