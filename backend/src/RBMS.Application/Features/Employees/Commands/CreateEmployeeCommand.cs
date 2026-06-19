using FluentValidation;
using MediatR;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;

namespace RBMS.Application.Features.Employees.Commands;

public record CreateEmployeeCommand(
    string EmployeeCode,
    string FullName,
    string Mobile,
    string? Email,
    string? Gender,
    DateOnly? DateOfBirth,
    string? Designation,
    string? Department,
    DateOnly JoiningDate,
    decimal MonthlyCtc,
    string? AddressLine1,
    string? City,
    string? State,
    string? Pincode,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? BankName,
    string? Ifsc,
    string? AccountLast4) : IRequest<Guid>, ITransactionalRequest;

public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Mobile).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.MonthlyCtc).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AccountLast4).MaximumLength(4);
    }
}

public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public CreateEmployeeCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? throw new ForbiddenAccessException("No tenant context.");

        var code = request.EmployeeCode.Trim();
        if (_uow.Repository<Employee>().Query().Any(e => e.EmployeeCode == code))
            throw new ConflictException($"Employee code '{code}' already exists.");

        var employee = new Employee
        {
            TenantId = tenantId,
            StoreId = _currentUser.StoreId,
            EmployeeCode = code,
            FullName = request.FullName.Trim(),
            Mobile = request.Mobile.Trim(),
            Email = request.Email,
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,
            Designation = request.Designation,
            Department = request.Department,
            JoiningDate = request.JoiningDate,
            MonthlyCtc = request.MonthlyCtc,
            AddressLine1 = request.AddressLine1,
            City = request.City,
            State = request.State,
            Pincode = request.Pincode,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            BankName = request.BankName,
            Ifsc = request.Ifsc,
            AccountLast4 = request.AccountLast4,
            Status = Domain.Enums.EmploymentStatus.Active
        };
        await _uow.Repository<Employee>().AddAsync(employee, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return employee.Id;
    }
}
