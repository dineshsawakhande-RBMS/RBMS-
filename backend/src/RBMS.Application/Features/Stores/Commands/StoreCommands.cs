using FluentValidation;
using MediatR;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;

namespace RBMS.Application.Features.Stores.Commands;

public record CreateStoreCommand(
    string Code, string Name, string? Gstin, string? Phone, string? Email,
    string? AddressLine1, string? City, string? State, string? Pincode)
    : IRequest<Guid>, ITransactionalRequest;

public class CreateStoreCommandValidator : AbstractValidator<CreateStoreCommand>
{
    public CreateStoreCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Gstin).MaximumLength(15);
    }
}

public class CreateStoreCommandHandler : IRequestHandler<CreateStoreCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public CreateStoreCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateStoreCommand request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId ?? throw new ForbiddenAccessException("No tenant context.");
        var code = request.Code.Trim();
        if (_uow.Repository<Store>().Query().Any(s => s.Code == code))
            throw new ConflictException($"Store code '{code}' already exists.");

        var store = new Store
        {
            TenantId = tenantId,
            Code = code,
            Name = request.Name.Trim(),
            Gstin = request.Gstin,
            Phone = request.Phone,
            Email = request.Email,
            AddressLine1 = request.AddressLine1,
            City = request.City,
            State = request.State,
            Pincode = request.Pincode,
            IsActive = true,
        };
        await _uow.Repository<Store>().AddAsync(store, ct);
        await _uow.SaveChangesAsync(ct);
        return store.Id;
    }
}

public record UpdateStoreCommand(
    Guid Id, string Name, string? Gstin, string? Phone, string? Email,
    string? AddressLine1, string? City, string? State, string? Pincode, bool IsActive)
    : IRequest, ITransactionalRequest;

public class UpdateStoreCommandValidator : AbstractValidator<UpdateStoreCommand>
{
    public UpdateStoreCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Gstin).MaximumLength(15);
    }
}

public class UpdateStoreCommandHandler : IRequestHandler<UpdateStoreCommand>
{
    private readonly IUnitOfWork _uow;
    public UpdateStoreCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(UpdateStoreCommand request, CancellationToken ct)
    {
        var s = await _uow.Repository<Store>().GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Store), request.Id);

        s.Name = request.Name.Trim();
        s.Gstin = request.Gstin;
        s.Phone = request.Phone;
        s.Email = request.Email;
        s.AddressLine1 = request.AddressLine1;
        s.City = request.City;
        s.State = request.State;
        s.Pincode = request.Pincode;
        s.IsActive = request.IsActive;

        _uow.Repository<Store>().Update(s);
        await _uow.SaveChangesAsync(ct);
    }
}

/// <summary>Soft-deletes a store (kept for audit; hidden from lists).</summary>
public record DeleteStoreCommand(Guid Id) : IRequest, ITransactionalRequest;

public class DeleteStoreCommandHandler : IRequestHandler<DeleteStoreCommand>
{
    private readonly IUnitOfWork _uow;
    public DeleteStoreCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(DeleteStoreCommand request, CancellationToken ct)
    {
        var s = await _uow.Repository<Store>().GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Store), request.Id);
        _uow.Repository<Store>().Remove(s);
        await _uow.SaveChangesAsync(ct);
    }
}
