using FluentValidation;
using MediatR;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Payroll.Commands;

// ---- record a salary advance (recovered automatically by future payroll runs) ----
public record CreateSalaryAdvanceCommand(Guid EmployeeId, decimal Amount, DateOnly AdvanceDate, string? Notes)
    : IRequest<Guid>, ITransactionalRequest;

public class CreateSalaryAdvanceCommandValidator : AbstractValidator<CreateSalaryAdvanceCommand>
{
    public CreateSalaryAdvanceCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public class CreateSalaryAdvanceCommandHandler : IRequestHandler<CreateSalaryAdvanceCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    public CreateSalaryAdvanceCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateSalaryAdvanceCommand request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId ?? throw new ForbiddenAccessException("No tenant context.");
        var advance = new SalaryAdvance
        {
            TenantId = tenantId,
            EmployeeId = request.EmployeeId,
            Amount = request.Amount,
            AdvanceDate = request.AdvanceDate,
            Recovered = 0,
            Notes = request.Notes,
        };
        await _uow.Repository<SalaryAdvance>().AddAsync(advance, ct);
        await _uow.SaveChangesAsync(ct);
        return advance.Id;
    }
}

// ---- mark a payroll run paid ----
public record MarkPayrollPaidCommand(Guid Id) : IRequest, ITransactionalRequest;

public class MarkPayrollPaidCommandHandler : IRequestHandler<MarkPayrollPaidCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly IDateTime _clock;
    public MarkPayrollPaidCommandHandler(IUnitOfWork uow, IDateTime clock)
    {
        _uow = uow;
        _clock = clock;
    }

    public async Task Handle(MarkPayrollPaidCommand request, CancellationToken ct)
    {
        var payroll = await _uow.Repository<Domain.Entities.Payroll>().GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Payroll), request.Id);
        payroll.Status = PayrollStatus.Paid;
        payroll.PaidAt = _clock.UtcNow;
        _uow.Repository<Domain.Entities.Payroll>().Update(payroll);
        await _uow.SaveChangesAsync(ct);
    }
}
