using FluentValidation;
using MediatR;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Employees.Commands;

public record UpdateEmployeeCommand(
    Guid Id,
    string FullName,
    string Mobile,
    string? Email,
    string? Designation,
    string? Department,
    decimal MonthlyCtc,
    EmploymentStatus Status,
    DateOnly? ExitDate,
    string? BankName,
    string? Ifsc,
    string? AccountLast4) : IRequest, ITransactionalRequest;

public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Mobile).NotEmpty().MaximumLength(20);
        RuleFor(x => x.MonthlyCtc).GreaterThanOrEqualTo(0);
    }
}

public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand>
{
    private readonly IUnitOfWork _uow;
    public UpdateEmployeeCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(UpdateEmployeeCommand request, CancellationToken ct)
    {
        var e = await _uow.Repository<Employee>().GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Employee), request.Id);

        e.FullName = request.FullName.Trim();
        e.Mobile = request.Mobile.Trim();
        e.Email = request.Email;
        e.Designation = request.Designation;
        e.Department = request.Department;
        e.MonthlyCtc = request.MonthlyCtc;
        e.Status = request.Status;
        e.ExitDate = request.ExitDate;
        e.BankName = request.BankName;
        e.Ifsc = request.Ifsc;
        e.AccountLast4 = request.AccountLast4;

        _uow.Repository<Employee>().Update(e);
        await _uow.SaveChangesAsync(ct);
    }
}

/// <summary>Soft-deletes an employee (flagged, hidden by the global filter, kept for audit).</summary>
public record DeleteEmployeeCommand(Guid Id) : IRequest, ITransactionalRequest;

public class DeleteEmployeeCommandHandler : IRequestHandler<DeleteEmployeeCommand>
{
    private readonly IUnitOfWork _uow;
    public DeleteEmployeeCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(DeleteEmployeeCommand request, CancellationToken ct)
    {
        var e = await _uow.Repository<Employee>().GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Employee), request.Id);
        _uow.Repository<Employee>().Remove(e);   // → soft delete via interceptor
        await _uow.SaveChangesAsync(ct);
    }
}
