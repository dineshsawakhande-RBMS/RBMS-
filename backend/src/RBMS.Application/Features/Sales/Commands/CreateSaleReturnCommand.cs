using FluentValidation;
using MediatR;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Sales.Commands;

/// <summary>Processes a sale return: stock comes back IN via the ledger and a refund is noted.</summary>
public record CreateSaleReturnCommand(
    Guid SaleId,
    Guid StoreId,
    string? Reason,
    PaymentMethod? RefundMethod,
    IReadOnlyList<SaleReturnItemInput> Items) : IRequest<Guid>, ITransactionalRequest;

public class CreateSaleReturnCommandValidator : AbstractValidator<CreateSaleReturnCommand>
{
    public CreateSaleReturnCommandValidator()
    {
        RuleFor(x => x.SaleId).NotEmpty();
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(l => l.VariantId).NotEmpty();
            i.RuleFor(l => l.Quantity).GreaterThan(0);
            i.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

public class CreateSaleReturnCommandHandler : IRequestHandler<CreateSaleReturnCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IStockLedger _ledger;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public CreateSaleReturnCommandHandler(
        IApplicationDbContext db, IStockLedger ledger, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _ledger = ledger;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Guid> Handle(CreateSaleReturnCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? throw new ForbiddenAccessException("No tenant context.");
        var now = _clock.UtcNow;
        var total = request.Items.Sum(i => i.Quantity * i.UnitPrice);

        var ret = new SaleReturn
        {
            TenantId = tenantId,
            StoreId = request.StoreId,
            SaleId = request.SaleId,
            ReturnNumber = $"SR-{now:yyyyMMddHHmmssfff}",
            ReturnDate = now,
            Reason = request.Reason,
            RefundMethod = request.RefundMethod,
            TotalAmount = total,
            Items = request.Items.Select(i => new SaleReturnItem
            {
                VariantId = i.VariantId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.Quantity * i.UnitPrice
            }).ToList()
        };
        _db.SaleReturns.Add(ret);

        foreach (var line in request.Items)
        {
            await _ledger.ApplyAsync(
                request.StoreId, line.VariantId, StockMovementType.SaleReturn, line.Quantity,
                referenceType: "SaleReturn", referenceId: ret.Id, notes: request.Reason, ct: cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ret.Id;
    }
}
