using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Interfaces;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Attendance.Queries;

public record GetMonthlyAttendanceQuery(Guid EmployeeId, int Year, int Month)
    : IRequest<IReadOnlyList<AttendanceDto>>;

public class GetMonthlyAttendanceQueryHandler
    : IRequestHandler<GetMonthlyAttendanceQuery, IReadOnlyList<AttendanceDto>>
{
    private readonly IApplicationDbContext _db;
    public GetMonthlyAttendanceQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<AttendanceDto>> Handle(GetMonthlyAttendanceQuery request, CancellationToken ct)
    {
        var (from, to) = MonthRange(request.Year, request.Month);
        return await _db.Attendance.AsNoTracking()
            .Where(a => a.EmployeeId == request.EmployeeId && a.WorkDate >= from && a.WorkDate <= to)
            .OrderBy(a => a.WorkDate)
            .Select(a => new AttendanceDto(
                a.Id, a.EmployeeId, a.WorkDate, a.Status, a.CheckIn, a.CheckOut, a.WorkedHours, a.Remarks))
            .ToListAsync(ct);
    }

    internal static (DateOnly From, DateOnly To) MonthRange(int year, int month)
    {
        var from = new DateOnly(year, month, 1);
        return (from, from.AddMonths(1).AddDays(-1));
    }
}

public record GetAttendanceSummaryQuery(Guid EmployeeId, int Year, int Month)
    : IRequest<AttendanceSummaryDto>;

public class GetAttendanceSummaryQueryHandler
    : IRequestHandler<GetAttendanceSummaryQuery, AttendanceSummaryDto>
{
    private readonly IApplicationDbContext _db;
    public GetAttendanceSummaryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<AttendanceSummaryDto> Handle(GetAttendanceSummaryQuery request, CancellationToken ct)
    {
        var (from, to) = GetMonthlyAttendanceQueryHandler.MonthRange(request.Year, request.Month);
        var rows = await _db.Attendance.AsNoTracking()
            .Where(a => a.EmployeeId == request.EmployeeId && a.WorkDate >= from && a.WorkDate <= to)
            .Select(a => a.Status)
            .ToListAsync(ct);

        int Count(AttendanceStatus s) => rows.Count(x => x == s);
        var present = Count(AttendanceStatus.Present);
        var absent = Count(AttendanceStatus.Absent);
        var halfDay = Count(AttendanceStatus.HalfDay);
        var leave = Count(AttendanceStatus.Leave);
        var holiday = Count(AttendanceStatus.Holiday);
        var weekOff = Count(AttendanceStatus.WeekOff);

        var workingDays = rows.Count(AttendanceMath.CountsAsWorkingDay);
        var presentDays = rows.Sum(AttendanceMath.PresentCredit);

        return new AttendanceSummaryDto(
            request.EmployeeId, request.Year, request.Month,
            workingDays, presentDays, present, absent, halfDay, leave, holiday, weekOff);
    }
}
