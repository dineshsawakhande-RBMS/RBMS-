using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Application.Features.Payroll.Queries;

public record SlipLineDto(string Name, bool IsEarning, decimal Amount);

public record SalarySlipDto(
    string BusinessName, string Currency, string EmployeeName, string EmployeeCode, string? Designation,
    int PeriodYear, int PeriodMonth, decimal WorkingDays, decimal PresentDays,
    decimal GrossEarnings, decimal TotalDeductions, decimal NetPay, IReadOnlyList<SlipLineDto> Lines);

public record GetPayrollSlipQuery(Guid Id) : IRequest<SalarySlipDto>;

public class GetPayrollSlipQueryHandler : IRequestHandler<GetPayrollSlipQuery, SalarySlipDto>
{
    private readonly IApplicationDbContext _db;
    public GetPayrollSlipQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<SalarySlipDto> Handle(GetPayrollSlipQuery request, CancellationToken ct)
    {
        var p = await _db.Payrolls.AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new
            {
                x.TenantId, x.Employee.FullName, x.Employee.EmployeeCode, x.Employee.Designation,
                x.PeriodYear, x.PeriodMonth, x.WorkingDays, x.PresentDays,
                x.GrossEarnings, x.Bonus, x.TotalDeductions, x.AdvanceDeducted, x.NetPay,
                Lines = x.Lines.Select(l => new SlipLineDto(
                    l.Name, l.Kind == Domain.Enums.SalaryComponentKind.Earning, l.Amount)).ToList()
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Payroll), request.Id);

        var tenant = await _db.Tenants.AsNoTracking()
            .Where(t => t.Id == p.TenantId).Select(t => new { t.Name, t.Currency }).FirstOrDefaultAsync(ct);

        return new SalarySlipDto(
            tenant?.Name ?? "RBMS", tenant?.Currency ?? "INR", p.FullName, p.EmployeeCode, p.Designation,
            p.PeriodYear, p.PeriodMonth, p.WorkingDays, p.PresentDays,
            p.GrossEarnings + p.Bonus, p.TotalDeductions + p.AdvanceDeducted, p.NetPay, p.Lines);
    }
}
