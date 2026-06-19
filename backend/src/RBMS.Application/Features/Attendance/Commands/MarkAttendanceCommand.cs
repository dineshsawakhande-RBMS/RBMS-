using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Attendance.Commands;

public record AttendanceEntryInput(
    DateOnly WorkDate, AttendanceStatus Status, TimeOnly? CheckIn, TimeOnly? CheckOut, string? Remarks);

/// <summary>Upserts one or more days of attendance for a single employee (e.g. a whole month at once).</summary>
public record MarkAttendanceCommand(Guid EmployeeId, IReadOnlyList<AttendanceEntryInput> Entries)
    : IRequest<int>, ITransactionalRequest;

public class MarkAttendanceCommandValidator : AbstractValidator<MarkAttendanceCommand>
{
    public MarkAttendanceCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Entries).NotEmpty();
        RuleForEach(x => x.Entries).ChildRules(e =>
        {
            e.RuleFor(i => i.WorkDate).NotEqual(default(DateOnly));
            e.RuleFor(i => i.Remarks).MaximumLength(500);
        });
        RuleFor(x => x.Entries)
            .Must(es => es.Select(e => e.WorkDate).Distinct().Count() == es.Count)
            .WithMessage("Duplicate dates in the same request.");
    }
}

public class MarkAttendanceCommandHandler : IRequestHandler<MarkAttendanceCommand, int>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public MarkAttendanceCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(MarkAttendanceCommand request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId ?? throw new ForbiddenAccessException("No tenant context.");

        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);

        foreach (var entry in request.Entries)
        {
            await AttendanceWriter.UpsertAsync(
                _db, tenantId, employee.StoreId, request.EmployeeId,
                entry.WorkDate, entry.Status, entry.CheckIn, entry.CheckOut, entry.Remarks,
                skipIfHolidayOrWeekOff: false, ct);
        }

        await _db.SaveChangesAsync(ct);
        return request.Entries.Count;
    }
}
