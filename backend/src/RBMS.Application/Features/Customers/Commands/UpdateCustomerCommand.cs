using FluentValidation;
using MediatR;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;

namespace RBMS.Application.Features.Customers.Commands;

public record UpdateCustomerCommand(
    Guid Id,
    string Name,
    string? Email,
    string? AddressLine1,
    string? City,
    string? State,
    string? Pincode,
    DateOnly? Birthday,
    DateOnly? Anniversary,
    bool IsActive) : IRequest, ITransactionalRequest;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand>
{
    private readonly IUnitOfWork _uow;
    public UpdateCustomerCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(UpdateCustomerCommand request, CancellationToken ct)
    {
        var c = await _uow.Repository<Customer>().GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Customer), request.Id);

        c.Name = request.Name.Trim();
        c.Email = request.Email;
        c.AddressLine1 = request.AddressLine1;
        c.City = request.City;
        c.State = request.State;
        c.Pincode = request.Pincode;
        c.Birthday = request.Birthday;
        c.Anniversary = request.Anniversary;
        c.IsActive = request.IsActive;

        _uow.Repository<Customer>().Update(c);
        await _uow.SaveChangesAsync(ct);
    }
}

public record DeleteCustomerCommand(Guid Id) : IRequest, ITransactionalRequest;

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand>
{
    private readonly IUnitOfWork _uow;
    public DeleteCustomerCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(DeleteCustomerCommand request, CancellationToken ct)
    {
        var c = await _uow.Repository<Customer>().GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Customer), request.Id);
        _uow.Repository<Customer>().Remove(c);
        await _uow.SaveChangesAsync(ct);
    }
}
