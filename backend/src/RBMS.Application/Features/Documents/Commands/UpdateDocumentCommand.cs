using FluentValidation;
using MediatR;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Documents.Commands;

/// <summary>Updates a document's metadata only — the stored file is immutable (re-upload to replace).</summary>
public record UpdateDocumentCommand(
    Guid Id,
    string Title,
    DocumentType DocumentType,
    string? Description,
    string? Tags,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string? RelatedEntityType,
    Guid? RelatedEntityId) : IRequest, ITransactionalRequest;

public class UpdateDocumentCommandValidator : AbstractValidator<UpdateDocumentCommand>
{
    public UpdateDocumentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Tags).MaximumLength(500);
        RuleFor(x => x.RelatedEntityType).MaximumLength(40);
        RuleFor(x => x.ExpiryDate)
            .GreaterThanOrEqualTo(x => x.IssueDate!.Value)
            .When(x => x.IssueDate.HasValue && x.ExpiryDate.HasValue)
            .WithMessage("Expiry date cannot be before the issue date.");
    }
}

public class UpdateDocumentCommandHandler : IRequestHandler<UpdateDocumentCommand>
{
    private readonly IUnitOfWork _uow;
    public UpdateDocumentCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(UpdateDocumentCommand request, CancellationToken ct)
    {
        var d = await _uow.Repository<Document>().GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Document), request.Id);

        d.Title = request.Title.Trim();
        d.DocumentType = request.DocumentType;
        d.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        d.Tags = DocumentTags.Normalize(request.Tags);
        d.IssueDate = request.IssueDate;
        d.ExpiryDate = request.ExpiryDate;
        d.RelatedEntityType = string.IsNullOrWhiteSpace(request.RelatedEntityType) ? null : request.RelatedEntityType.Trim();
        d.RelatedEntityId = request.RelatedEntityId;

        _uow.Repository<Document>().Update(d);
        await _uow.SaveChangesAsync(ct);
    }
}

/// <summary>Soft-deletes a document (flagged, hidden by the global filter; the stored file is kept).</summary>
public record DeleteDocumentCommand(Guid Id) : IRequest, ITransactionalRequest;

public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand>
{
    private readonly IUnitOfWork _uow;
    public DeleteDocumentCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(DeleteDocumentCommand request, CancellationToken ct)
    {
        var d = await _uow.Repository<Document>().GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Document), request.Id);
        _uow.Repository<Document>().Remove(d);   // → soft delete via interceptor
        await _uow.SaveChangesAsync(ct);
    }
}
