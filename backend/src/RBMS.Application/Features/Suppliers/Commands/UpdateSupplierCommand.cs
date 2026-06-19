using FluentValidation;
using MediatR;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;

namespace RBMS.Application.Features.Suppliers.Commands;

public record UpdateSupplierCommand(
    Guid Id,
    string Name,
    string? Gstin,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? AddressLine1,
    string? City,
    string? State,
    string? Pincode,
    int PaymentTermsDays,
    bool IsActive) : IRequest, ITransactionalRequest;

public class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Gstin).MaximumLength(15);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.PaymentTermsDays).GreaterThanOrEqualTo(0);
    }
}

public class UpdateSupplierCommandHandler : IRequestHandler<UpdateSupplierCommand>
{
    private readonly IUnitOfWork _uow;
    public UpdateSupplierCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(UpdateSupplierCommand request, CancellationToken ct)
    {
        var s = await _uow.Repository<Supplier>().GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Supplier), request.Id);

        s.Name = request.Name.Trim();
        s.Gstin = request.Gstin;
        s.ContactPerson = request.ContactPerson;
        s.Phone = request.Phone;
        s.Email = request.Email;
        s.AddressLine1 = request.AddressLine1;
        s.City = request.City;
        s.State = request.State;
        s.Pincode = request.Pincode;
        s.PaymentTermsDays = request.PaymentTermsDays;
        s.IsActive = request.IsActive;

        _uow.Repository<Supplier>().Update(s);
        await _uow.SaveChangesAsync(ct);
    }
}

public record DeleteSupplierCommand(Guid Id) : IRequest, ITransactionalRequest;

public class DeleteSupplierCommandHandler : IRequestHandler<DeleteSupplierCommand>
{
    private readonly IUnitOfWork _uow;
    public DeleteSupplierCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(DeleteSupplierCommand request, CancellationToken ct)
    {
        var s = await _uow.Repository<Supplier>().GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Supplier), request.Id);
        _uow.Repository<Supplier>().Remove(s);
        await _uow.SaveChangesAsync(ct);
    }
}
