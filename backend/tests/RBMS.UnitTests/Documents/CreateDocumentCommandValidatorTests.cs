using FluentValidation.TestHelper;
using RBMS.Application.Features.Documents;
using RBMS.Application.Features.Documents.Commands;
using RBMS.Domain.Enums;
using Xunit;

namespace RBMS.UnitTests.Documents;

public class CreateDocumentCommandValidatorTests
{
    private readonly CreateDocumentCommandValidator _validator = new();

    private static CreateDocumentCommand Valid() => new(
        Title: "GST Registration Certificate",
        DocumentType: DocumentType.GstCertificate,
        Description: "Issued by GST dept",
        Tags: "gst,legal",
        FileKey: "documents/abc/def.pdf",
        FileName: "gst-cert.pdf",
        ContentType: "application/pdf",
        FileSizeBytes: 12345,
        IssueDate: new DateOnly(2024, 1, 1),
        ExpiryDate: new DateOnly(2027, 1, 1),
        RelatedEntityType: null,
        RelatedEntityId: null);

    [Fact]
    public void Valid_command_passes()
        => _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Empty_title_fails()
        => _validator.TestValidate(Valid() with { Title = "" })
            .ShouldHaveValidationErrorFor(c => c.Title);

    [Fact]
    public void Missing_file_key_fails()
        => _validator.TestValidate(Valid() with { FileKey = "" })
            .ShouldHaveValidationErrorFor(c => c.FileKey);

    [Fact]
    public void Zero_size_fails()
        => _validator.TestValidate(Valid() with { FileSizeBytes = 0 })
            .ShouldHaveValidationErrorFor(c => c.FileSizeBytes);

    [Fact]
    public void Expiry_before_issue_fails()
        => _validator.TestValidate(Valid() with
            {
                IssueDate = new DateOnly(2027, 1, 1),
                ExpiryDate = new DateOnly(2024, 1, 1)
            })
            .ShouldHaveValidationErrorFor(c => c.ExpiryDate);

    [Theory]
    [InlineData("GST, Legal ,gst", "gst,legal")]
    [InlineData("  ", null)]
    [InlineData(null, null)]
    public void Tags_normalize_trims_lowercases_and_dedupes(string? raw, string? expected)
        => Assert.Equal(expected, DocumentTags.Normalize(raw));
}
