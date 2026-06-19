using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Attendance.Commands;

public record CreateLeaveRequestCommand(
    Guid EmployeeId, LeaveType LeaveType, DateOnly FromDate, DateOnly ToDate, string? Reason)
    : IRequest<Guid>, ITransactionalRequest;

public class CreateLeaveRequestCommandValidator : AbstractValidator<CreateLeaveRequestCommand>
{
    public CreateLeaveRequestCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("End date cannot be before start date.");
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}

public class CreateLeaveRequestCommandHandler : IRequestHandler<CreateLeaveRequestCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateLeaveRequestCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateLeaveRequestCommand request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId ?? throw new ForbiddenAccessException("No tenant context.");

        if (!await _db.Employees.AnyAsync(e => e.Id == request.EmployeeId, ct))
            throw new NotFoundException(nameof(Employee), request.EmployeeId);

        var days = request.ToDate.DayNumber - request.FromDate.DayNumber + 1;
        var leave = new LeaveRequest
        {
            TenantId = tenantId,
            EmployeeId = request.EmployeeId,
            LeaveType = request.LeaveType,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Days = days,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            Status = LeaveStatus.Pending,
        };
        _db.Leaves.Add(leave);
        await _db.SaveChangesAsync(ct);
        return leave.Id;
    }
}

/// <summary>Approves or rejects a pending leave. Approval auto-marks attendance as Leave across the range.</summary>
public record DecideLeaveRequestCommand(Guid Id, bool Approve, string? DecisionNotes)
    : IRequest, ITransactionalRequest;

public class DecideLeaveRequestCommandValidator : AbstractValidator<DecideLeaveRequestCommand>
{
    public DecideLeaveRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DecisionNotes).MaximumLength(1000);
    }
}

public class DecideLeaveRequestCommandHandler : IRequestHandler<DecideLeaveRequestCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public DecideLeaveRequestCommandHandler(IApplicationDbContext db, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task Handle(DecideLeaveRequestCommand request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId ?? throw new ForbiddenAccessException("No tenant context.");

        var leave = await _db.Leaves.FirstOrDefaultAsync(l => l.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(LeaveRequest), request.Id);

        if (leave.Status != LeaveStatus.Pending)
            throw new ConflictException("Only a pending leave request can be decided.");

        leave.Status = request.Approve ? LeaveStatus.Approved : LeaveStatus.Rejected;
        leave.ApprovedBy = _currentUser.UserId;
        leave.ApprovedAt = _clock.UtcNow;
        leave.DecisionNotes = string.IsNullOrWhiteSpace(request.DecisionNotes) ? null : request.DecisionNotes.Trim();

        if (request.Approve)
        {
            var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == leave.EmployeeId, ct);
            var remark = $"Leave ({leave.LeaveType})";
            for (var d = leave.FromDate; d <= leave.ToDate; d = d.AddDays(1))
            {
                await AttendanceWriter.UpsertAsync(
                    _db, tenantId, employee?.StoreId, leave.EmployeeId,
                    d, AttendanceStatus.Leave, null, null, remark,
                    skipIfHolidayOrWeekOff: true, ct);
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
