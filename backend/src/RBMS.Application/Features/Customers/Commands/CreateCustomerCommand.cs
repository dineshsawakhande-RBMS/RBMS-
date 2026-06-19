using FluentValidation;
using MediatR;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;

namespace RBMS.Application.Features.Customers.Commands;

public record CreateCustomerCommand(
    string Name,
    string Mobile,
    string? Email,
    string? AddressLine1,
    string? City,
    string? State,
    string? Pincode,
    DateOnly? Birthday,
    DateOnly? Anniversary) : IRequest<Guid>, ITransactionalRequest;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Mobile).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public CreateCustomerCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? throw new ForbiddenAccessException("No tenant context.");

        var mobile = request.Mobile.Trim();
        if (_uow.Repository<Customer>().Query().Any(c => c.Mobile == mobile))
            throw new ConflictException($"A customer with mobile '{mobile}' already exists.");

        var customer = new Customer
        {
            TenantId = tenantId,
            Name = request.Name.Trim(),
            Mobile = mobile,
            Email = request.Email,
            AddressLine1 = request.AddressLine1,
            City = request.City,
            State = request.State,
            Pincode = request.Pincode,
            Birthday = request.Birthday,
            Anniversary = request.Anniversary,
            LoyaltyPoints = 0,
            IsActive = true
        };
        await _uow.Repository<Customer>().AddAsync(customer, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return customer.Id;
    }
}
