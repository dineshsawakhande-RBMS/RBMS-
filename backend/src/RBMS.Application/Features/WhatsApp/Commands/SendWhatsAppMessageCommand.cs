using FluentValidation;
using MediatR;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.WhatsApp.Commands;

public record SendWhatsAppMessageCommand(
    string ToPhone, string? RecipientName, WhatsAppMessageKind Kind, string Body,
    string? RelatedEntityType, Guid? RelatedEntityId) : IRequest<Guid>, ITransactionalRequest;

public class SendWhatsAppMessageCommandValidator : AbstractValidator<SendWhatsAppMessageCommand>
{
    public SendWhatsAppMessageCommandValidator()
    {
        RuleFor(x => x.ToPhone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.RecipientName).MaximumLength(200);
    }
}

public class SendWhatsAppMessageCommandHandler : IRequestHandler<SendWhatsAppMessageCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IWhatsAppSender _sender;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public SendWhatsAppMessageCommandHandler(
        IApplicationDbContext db, IWhatsAppSender sender, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _sender = sender;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Guid> Handle(SendWhatsAppMessageCommand request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId ?? throw new ForbiddenAccessException("No tenant context.");
        var msg = await WhatsAppDispatcher.DispatchAsync(
            _db, _sender, _clock, tenantId, _currentUser.StoreId,
            request.ToPhone, request.RecipientName, request.Kind, request.Body.Trim(),
            request.RelatedEntityType, request.RelatedEntityId, ct);
        await _db.SaveChangesAsync(ct);
        return msg.Id;
    }
}
