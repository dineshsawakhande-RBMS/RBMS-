using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Application.Features.Payroll.Queries;

// ---- payroll runs for a period ----
public record GetPayrollsQuery(int Year, int Month) : IRequest<IReadOnlyList<PayrollListItemDto>>;

public class GetPayrollsQueryHandler : IRequestHandler<GetPayrollsQuery, IReadOnlyList<PayrollListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public GetPayrollsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<PayrollListItemDto>> Handle(GetPayrollsQuery request, CancellationToken ct)
        => await _db.Payrolls.AsNoTracking()
            .Where(p => p.PeriodYear == request.Year && p.PeriodMonth == request.Month)
            .OrderBy(p => p.Employee.FullName)
            .Select(p => new PayrollListItemDto(
                p.Id, p.Employee.FullName, p.PeriodYear, p.PeriodMonth,
                p.GrossEarnings, p.TotalDeductions + p.AdvanceDeducted, p.NetPay, p.Status))
            .ToListAsync(ct);
}

// ---- payroll detail ----
public record GetPayrollByIdQuery(Guid Id) : IRequest<PayrollDto>;

public class GetPayrollByIdQueryHandler : IRequestHandler<GetPayrollByIdQuery, PayrollDto>
{
    private readonly IApplicationDbContext _db;
    public GetPayrollByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PayrollDto> Handle(GetPayrollByIdQuery request, CancellationToken ct)
    {
        var p = await _db.Payrolls.AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new PayrollDto(
                x.Id, x.Employee.FullName, x.Employee.EmployeeCode, x.PeriodYear, x.PeriodMonth,
                x.WorkingDays, x.PresentDays, x.GrossEarnings, x.Bonus, x.TotalDeductions,
                x.AdvanceDeducted, x.NetPay, x.Status,
                x.Lines.Select(l => new PayrollLineDto(l.Name, l.Kind, l.Amount)).ToList()))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Payroll), request.Id);
        return p;
    }
}

// ---- advances ----
public record GetSalaryAdvancesQuery(Guid? EmployeeId = null) : IRequest<IReadOnlyList<SalaryAdvanceDto>>;

public class GetSalaryAdvancesQueryHandler : IRequestHandler<GetSalaryAdvancesQuery, IReadOnlyList<SalaryAdvanceDto>>
{
    private readonly IApplicationDbContext _db;
    public GetSalaryAdvancesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<SalaryAdvanceDto>> Handle(GetSalaryAdvancesQuery request, CancellationToken ct)
    {
        var query = _db.SalaryAdvances.AsNoTracking();
        if (request.EmployeeId is { } eid) query = query.Where(a => a.EmployeeId == eid);
        return await query
            .OrderByDescending(a => a.AdvanceDate)
            .Select(a => new SalaryAdvanceDto(
                a.Id, a.Employee.FullName, a.Amount, a.AdvanceDate, a.Recovered, a.Amount - a.Recovered, a.Notes))
            .ToListAsync(ct);
    }
}
