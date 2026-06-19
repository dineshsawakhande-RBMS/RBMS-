using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Interfaces;
using RBMS.Domain.Enums;
using DomainAttendance = RBMS.Domain.Entities.Attendance;

namespace RBMS.Application.Features.Attendance;

/// <summary>Upserts attendance by (employee, date). Used by bulk-mark and leave-approval; the
/// caller owns the transaction and calls SaveChanges once.</summary>
internal static class AttendanceWriter
{
    public static async Task UpsertAsync(
        IApplicationDbContext db, Guid tenantId, Guid? storeId, Guid employeeId,
        DateOnly workDate, AttendanceStatus status,
        TimeOnly? checkIn, TimeOnly? checkOut, string? remarks,
        bool skipIfHolidayOrWeekOff, CancellationToken ct)
    {
        var existing = await db.Attendance
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate == workDate, ct);

        if (existing is null)
        {
            db.Attendance.Add(new DomainAttendance
            {
                TenantId = tenantId,
                StoreId = storeId,
                EmployeeId = employeeId,
                WorkDate = workDate,
                Status = status,
                CheckIn = checkIn,
                CheckOut = checkOut,
                Remarks = remarks,
            });
            return;
        }

        // Don't clobber an explicit holiday / weekly-off when auto-marking leave.
        if (skipIfHolidayOrWeekOff && existing.Status is AttendanceStatus.Holiday or AttendanceStatus.WeekOff)
            return;

        existing.Status = status;
        existing.CheckIn = checkIn;
        existing.CheckOut = checkOut;
        existing.Remarks = remarks;
    }
}
