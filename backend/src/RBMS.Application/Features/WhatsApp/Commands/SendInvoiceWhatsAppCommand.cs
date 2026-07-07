using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.WhatsApp.Commands;

/// <summary>Sends the invoice summary for a completed sale to its customer over WhatsApp.</summary>
public record SendInvoiceWhatsAppCommand(Guid SaleId) : IRequest<Guid>, ITransactionalRequest;

public class SendInvoiceWhatsAppCommandHandler : IRequestHandler<SendInvoiceWhatsAppCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IWhatsAppSender _sender;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public SendInvoiceWhatsAppCommandHandler(
        IApplicationDbContext db, IWhatsAppSender sender, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _sender = sender;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Guid> Handle(SendInvoiceWhatsAppCommand request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId ?? throw new ForbiddenAccessException("No tenant context.");

        var sale = await _db.Sales.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.SaleId, ct)
            ?? throw new NotFoundException(nameof(Sale), request.SaleId);

        if (sale.CustomerId is null)
            throw new ConflictException("This sale has no customer to message.");

        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == sale.CustomerId, ct)
            ?? throw new NotFoundException(nameof(Customer), sale.CustomerId);

        var body =
            $"Hi {customer.Name}, thank you for shopping with us! " +
            $"Invoice {sale.InvoiceNumber} dated {sale.InvoiceDate:yyyy-MM-dd} — total ₹{sale.GrandTotal:0.00}. " +
            "We hope to see you again soon.";

        var msg = await WhatsAppDispatcher.DispatchAsync(
            _db, _sender, _clock, tenantId, sale.StoreId,
            customer.Mobile, customer.Name, WhatsAppMessageKind.Invoice, body,
            "Sale", sale.Id, ct);
        await _db.SaveChangesAsync(ct);
        return msg.Id;
    }
}
