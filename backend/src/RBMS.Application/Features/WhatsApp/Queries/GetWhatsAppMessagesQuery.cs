using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.WhatsApp.Queries;

public record GetWhatsAppMessagesQuery(WhatsAppMessageStatus? Status = null, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<WhatsAppMessageDto>>;

public class GetWhatsAppMessagesQueryHandler
    : IRequestHandler<GetWhatsAppMessagesQuery, PagedResult<WhatsAppMessageDto>>
{
    private readonly IApplicationDbContext _db;
    public GetWhatsAppMessagesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<WhatsAppMessageDto>> Handle(GetWhatsAppMessagesQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 100);

        var query = _db.WhatsAppMessages.AsNoTracking();
        if (request.Status.HasValue) query = query.Where(m => m.Status == request.Status.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(m => new WhatsAppMessageDto(
                m.Id, m.ToPhone, m.RecipientName, m.Kind, m.Body, m.Status, m.Provider,
                m.ProviderMessageId, m.Error, m.SentAt, m.RelatedEntityType, m.RelatedEntityId, m.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<WhatsAppMessageDto>(items, total, page, size);
    }
}
