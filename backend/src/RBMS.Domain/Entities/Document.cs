using RBMS.Domain.Common;
using RBMS.Domain.Enums;

namespace RBMS.Domain.Entities;

/// <summary>
/// A stored business document (GST certificate, rent agreement, supplier/employee paperwork…).
/// The file itself lives in <see cref="Common.Interfaces.IFileStorage"/> under <see cref="FileKey"/>;
/// this row carries the searchable metadata, tags and an optional expiry for alerting.
/// </summary>
public class Document : AuditableEntity
{
    public Guid? StoreId { get; set; }
    public string Title { get; set; } = null!;
    public DocumentType DocumentType { get; set; }
    public string? Description { get; set; }

    /// <summary>Normalized, comma-separated, lowercase tags — searched with a simple contains.</summary>
    public string? Tags { get; set; }

    /// <summary>Storage key (e.g. <c>documents/{guid}/{guid}.pdf</c>).</summary>
    public string FileKey { get; set; } = null!;

    /// <summary>Original uploaded file name (shown to users, used for download).</summary>
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long FileSizeBytes { get; set; }

    public DateOnly? IssueDate { get; set; }

    /// <summary>Optional expiry — drives the "expiring soon" alert query (feeds Notifications later).</summary>
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>Optional soft link to another module, e.g. "Supplier" or "Employee" (no FK).</summary>
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
}
