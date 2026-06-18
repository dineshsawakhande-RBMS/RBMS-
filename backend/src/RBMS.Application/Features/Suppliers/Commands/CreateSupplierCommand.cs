using FluentValidation;
using MediatR;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;

namespace RBMS.Application.Features.Suppliers.Commands;

public record CreateSupplierCommand(
    string Code,
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
    decimal OpeningBalance) : IRequest<Guid>, ITransactionalRequest;

public class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Gstin).MaximumLength(15);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.PaymentTermsDays).GreaterThanOrEqualTo(0);
    }
}

public class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public CreateSupplierCommandHandler(IUnitOfWork uow, ICurrentUser currentUser, IDateTime clock)
    {
        _uow = uow;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Guid> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? throw new ForbiddenAccessException("No tenant context.");

        var code = request.Code.Trim();
        if (_uow.Repository<Supplier>().Query().Any(s => s.Code == code))
            throw new ConflictException($"Supplier code '{code}' already exists.");

        var supplier = new Supplier
        {
            TenantId = tenantId,
            Code = code,
            Name = request.Name.Trim(),
            Gstin = request.Gstin,
            ContactPerson = request.ContactPerson,
            Phone = request.Phone,
            Email = request.Email,
            AddressLine1 = request.AddressLine1,
            City = request.City,
            State = request.State,
            Pincode = request.Pincode,
            PaymentTermsDays = request.PaymentTermsDays,
            OpeningBalance = request.OpeningBalance,
            IsActive = true
        };
        await _uow.Repository<Supplier>().AddAsync(supplier, cancellationToken);

        // Seed the ledger with the opening balance so the running balance ties out.
        if (request.OpeningBalance != 0)
        {
            await _uow.Repository<SupplierLedgerEntry>().AddAsync(new SupplierLedgerEntry
            {
                TenantId = tenantId,
                SupplierId = supplier.Id,
                EntryDate = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime),
                ReferenceType = "Opening",
                Credit = request.OpeningBalance > 0 ? request.OpeningBalance : 0,
                Debit = request.OpeningBalance < 0 ? -request.OpeningBalance : 0,
                Notes = "Opening balance",
                CreatedAt = _clock.UtcNow,
                CreatedBy = _currentUser.UserId
            }, cancellationToken);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return supplier.Id;
    }
}
