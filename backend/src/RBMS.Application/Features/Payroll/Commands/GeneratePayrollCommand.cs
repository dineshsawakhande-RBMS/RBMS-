using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Payroll.Commands;

/// <summary>
/// Generates a monthly payroll run for an employee: gross is the CTC prorated by
/// attendance (present/working days), plus bonus, minus deductions and auto-recovery of
/// any outstanding salary advances. One run per employee per month.
/// </summary>
public record GeneratePayrollCommand(
    Guid EmployeeId,
    int PeriodYear,
    int PeriodMonth,
    decimal WorkingDays,
    decimal PresentDays,
    decimal Bonus,
    decimal Deductions) : IRequest<Guid>, ITransactionalRequest;

public class GeneratePayrollCommandValidator : AbstractValidator<GeneratePayrollCommand>
{
    public GeneratePayrollCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.PeriodYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.PeriodMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.WorkingDays).GreaterThan(0).LessThanOrEqualTo(31);
        RuleFor(x => x.PresentDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PresentDays).LessThanOrEqualTo(x => x.WorkingDays)
            .WithMessage("Present days cannot exceed working days.");
        RuleFor(x => x.Bonus).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Deductions).GreaterThanOrEqualTo(0);
    }
}

public class GeneratePayrollCommandHandler : IRequestHandler<GeneratePayrollCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GeneratePayrollCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(GeneratePayrollCommand request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId ?? throw new ForbiddenAccessException("No tenant context.");

        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);

        var exists = await _db.Payrolls.AnyAsync(
            p => p.EmployeeId == request.EmployeeId && p.PeriodYear == request.PeriodYear && p.PeriodMonth == request.PeriodMonth, ct);
        if (exists)
            throw new ConflictException($"Payroll for {request.PeriodMonth:00}/{request.PeriodYear} already exists for this employee.");

        var gross = Math.Round(employee.MonthlyCtc * request.PresentDays / request.WorkingDays, 2);
        var beforeAdvance = gross + request.Bonus - request.Deductions;

        // Auto-recover outstanding advances (FIFO), capped at the pay available.
        var recoverable = Math.Max(0, beforeAdvance);
        decimal advanceDeducted = 0;
        var advances = await _db.SalaryAdvances
            .Where(a => a.EmployeeId == request.EmployeeId && a.Recovered < a.Amount)
            .OrderBy(a => a.AdvanceDate)
            .ToListAsync(ct);
        foreach (var adv in advances)
        {
            if (recoverable <= 0) break;
            var take = Math.Min(adv.Amount - adv.Recovered, recoverable);
            adv.Recovered += take;
            advanceDeducted += take;
            recoverable -= take;
        }

        var netPay = beforeAdvance - advanceDeducted;

        var lines = new List<PayrollLine>
        {
            new() { Name = "Gross (attendance-prorated)", Kind = SalaryComponentKind.Earning, Amount = gross },
        };
        if (request.Bonus > 0) lines.Add(new() { Name = "Bonus", Kind = SalaryComponentKind.Earning, Amount = request.Bonus });
        if (request.Deductions > 0) lines.Add(new() { Name = "Deductions", Kind = SalaryComponentKind.Deduction, Amount = request.Deductions });
        if (advanceDeducted > 0) lines.Add(new() { Name = "Advance recovery", Kind = SalaryComponentKind.Deduction, Amount = advanceDeducted });

        var payroll = new Domain.Entities.Payroll
        {
            TenantId = tenantId,
            EmployeeId = request.EmployeeId,
            PeriodYear = request.PeriodYear,
            PeriodMonth = request.PeriodMonth,
            WorkingDays = request.WorkingDays,
            PresentDays = request.PresentDays,
            GrossEarnings = gross,
            Bonus = request.Bonus,
            TotalDeductions = request.Deductions,
            AdvanceDeducted = advanceDeducted,
            NetPay = netPay,
            Status = PayrollStatus.Generated,
            Lines = lines,
        };
        _db.Payrolls.Add(payroll);
        await _db.SaveChangesAsync(ct);
        return payroll.Id;
    }
}
