using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Attendance.Queries;

public record GetLeaveRequestsQuery(
    Guid? EmployeeId = null, LeaveStatus? Status = null, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<LeaveRequestDto>>;

public class GetLeaveRequestsQueryHandler
    : IRequestHandler<GetLeaveRequestsQuery, PagedResult<LeaveRequestDto>>
{
    private readonly IApplicationDbContext _db;
    public GetLeaveRequestsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<LeaveRequestDto>> Handle(GetLeaveRequestsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 100);

        var query = _db.Leaves.AsNoTracking();
        if (request.EmployeeId.HasValue)
            query = query.Where(l => l.EmployeeId == request.EmployeeId.Value);
        if (request.Status.HasValue)
            query = query.Where(l => l.Status == request.Status.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(l => l.FromDate)
            .Skip((page - 1) * size).Take(size)
            .Select(l => new LeaveRequestDto(
                l.Id, l.EmployeeId, l.Employee.FullName, l.LeaveType,
                l.FromDate, l.ToDate, l.Days, l.Reason, l.Status, l.ApprovedAt, l.DecisionNotes))
            .ToListAsync(ct);

        return new PagedResult<LeaveRequestDto>(items, total, page, size);
    }
}
