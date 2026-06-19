using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RBMS.Application.Common.Models;
using RBMS.Application.Features.Auth;
using RBMS.Application.Features.Documents;
using RBMS.Application.Features.Documents.Commands;
using RBMS.Domain.Enums;
using Xunit;

namespace RBMS.IntegrationTests.Documents;

public class DocumentFlowTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DocumentFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.SeedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task AuthenticateAsync()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginCommand(CustomWebApplicationFactory.Seed.Username, CustomWebApplicationFactory.Seed.Password));
        var auth = await login.Content.ReadFromJsonAsync<AuthResultDto>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    private async Task<Guid> UploadAsync(
        string title, string type = "GstCertificate", string? tags = null,
        string? expiryDate = null, string fileName = "doc.pdf", string contentType = "application/pdf")
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // "%PDF"
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(title), "title");
        form.Add(new StringContent(type), "documentType");
        if (tags is not null) form.Add(new StringContent(tags), "tags");
        if (expiryDate is not null) form.Add(new StringContent(expiryDate), "expiryDate");

        var resp = await _client.PostAsync("/api/documents", form);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return await resp.Content.ReadFromJsonAsync<Guid>();
    }

    [Fact]
    public async Task Upload_then_fetch_document()
    {
        await AuthenticateAsync();
        var id = await UploadAsync("GST Certificate", tags: "GST, Legal ,gst");

        var detail = await _client.GetFromJsonAsync<DocumentDto>($"/api/documents/{id}", TestJson.Options);
        detail!.Title.Should().Be("GST Certificate");
        detail.DocumentType.Should().Be(DocumentType.GstCertificate);
        detail.FileName.Should().Be("doc.pdf");
        detail.ContentType.Should().Be("application/pdf");
        detail.FileSizeBytes.Should().Be(4);
        detail.Tags.Should().BeEquivalentTo(new[] { "gst", "legal" });
        detail.DownloadUrl.Should().StartWith("/uploads/documents/");
    }

    [Fact]
    public async Task Rejects_disallowed_content_type()
    {
        await AuthenticateAsync();
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-msdownload");
        form.Add(fileContent, "file", "evil.exe");
        form.Add(new StringContent("Bad"), "title");
        form.Add(new StringContent("Other"), "documentType");

        var resp = await _client.PostAsync("/api/documents", form);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_and_type_filter_work()
    {
        await AuthenticateAsync();
        var marker = Guid.NewGuid().ToString("N").Substring(0, 8);
        await UploadAsync($"Rent Agreement {marker}", type: "RentAgreement");

        var bySearch = await _client.GetFromJsonAsync<PagedResult<DocumentListItemDto>>(
            $"/api/documents?search={marker}", TestJson.Options);
        bySearch!.Items.Should().ContainSingle(d => d.Title.Contains(marker));

        var byType = await _client.GetFromJsonAsync<PagedResult<DocumentListItemDto>>(
            $"/api/documents?documentType=RentAgreement&search={marker}", TestJson.Options);
        byType!.Items.Should().OnlyContain(d => d.DocumentType == DocumentType.RentAgreement);
    }

    [Fact]
    public async Task Expiring_query_includes_past_expiry_and_excludes_far_future()
    {
        await AuthenticateAsync();
        var expiringId = await UploadAsync("Expired License", type: "License", expiryDate: "2020-01-01");
        var farId = await UploadAsync("Future License", type: "License", expiryDate: "2099-01-01");

        var expiring = await _client.GetFromJsonAsync<List<DocumentListItemDto>>(
            "/api/documents/expiring?withinDays=30", TestJson.Options);

        expiring!.Should().Contain(d => d.Id == expiringId);
        expiring.Should().NotContain(d => d.Id == farId);
    }

    [Fact]
    public async Task Update_changes_metadata()
    {
        await AuthenticateAsync();
        var id = await UploadAsync("Draft Title");

        var update = await _client.PutAsJsonAsync($"/api/documents/{id}", new UpdateDocumentCommand(
            Id: id, Title: "Final Title", DocumentType: DocumentType.Contract, Description: "signed",
            Tags: "contract", IssueDate: null, ExpiryDate: null, RelatedEntityType: null, RelatedEntityId: null));
        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await _client.GetFromJsonAsync<DocumentDto>($"/api/documents/{id}", TestJson.Options);
        detail!.Title.Should().Be("Final Title");
        detail.DocumentType.Should().Be(DocumentType.Contract);
    }

    [Fact]
    public async Task Soft_deleted_document_disappears()
    {
        await AuthenticateAsync();
        var id = await UploadAsync("To Delete");

        var del = await _client.DeleteAsync($"/api/documents/{id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var fetch = await _client.GetAsync($"/api/documents/{id}");
        fetch.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
