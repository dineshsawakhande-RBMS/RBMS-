using FluentValidation;
using MediatR;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Documents.Commands;

/// <summary>
/// Records a document's metadata. The controller uploads the file to <see cref="IFileStorage"/>
/// first, then sends this with the resulting <paramref name="FileKey"/> (mirrors product images).
/// </summary>
public record CreateDocumentCommand(
    string Title,
    DocumentType DocumentType,
    string? Description,
    string? Tags,
    string FileKey,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string? RelatedEntityType,
    Guid? RelatedEntityId) : IRequest<Guid>, ITransactionalRequest;

public class CreateDocumentCommandValidator : AbstractValidator<CreateDocumentCommand>
{
    public CreateDocumentCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.FileKey).NotEmpty().MaximumLength(500);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(150);
        RuleFor(x => x.FileSizeBytes).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Tags).MaximumLength(500);
        RuleFor(x => x.RelatedEntityType).MaximumLength(40);
        RuleFor(x => x.ExpiryDate)
            .GreaterThanOrEqualTo(x => x.IssueDate!.Value)
            .When(x => x.IssueDate.HasValue && x.ExpiryDate.HasValue)
            .WithMessage("Expiry date cannot be before the issue date.");
    }
}

public class CreateDocumentCommandHandler : IRequestHandler<CreateDocumentCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public CreateDocumentCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateDocumentCommand request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId ?? throw new ForbiddenAccessException("No tenant context.");

        var document = new Document
        {
            TenantId = tenantId,
            StoreId = _currentUser.StoreId,
            Title = request.Title.Trim(),
            DocumentType = request.DocumentType,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Tags = DocumentTags.Normalize(request.Tags),
            FileKey = request.FileKey,
            FileName = request.FileName,
            ContentType = request.ContentType,
            FileSizeBytes = request.FileSizeBytes,
            IssueDate = request.IssueDate,
            ExpiryDate = request.ExpiryDate,
            RelatedEntityType = string.IsNullOrWhiteSpace(request.RelatedEntityType) ? null : request.RelatedEntityType.Trim(),
            RelatedEntityId = request.RelatedEntityId,
        };
        await _uow.Repository<Document>().AddAsync(document, ct);
        await _uow.SaveChangesAsync(ct);
        return document.Id;
    }
}
