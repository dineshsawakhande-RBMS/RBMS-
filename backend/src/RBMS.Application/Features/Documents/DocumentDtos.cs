using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Documents;

public record DocumentListItemDto(
    Guid Id, string Title, DocumentType DocumentType, string FileName, string ContentType,
    long FileSizeBytes, IReadOnlyList<string> Tags, DateOnly? IssueDate, DateOnly? ExpiryDate,
    string? RelatedEntityType, Guid? RelatedEntityId, string DownloadUrl, DateTimeOffset CreatedAt);

public record DocumentDto(
    Guid Id, string Title, DocumentType DocumentType, string? Description, IReadOnlyList<string> Tags,
    string FileName, string ContentType, long FileSizeBytes, DateOnly? IssueDate, DateOnly? ExpiryDate,
    string? RelatedEntityType, Guid? RelatedEntityId, string DownloadUrl, DateTimeOffset CreatedAt);
