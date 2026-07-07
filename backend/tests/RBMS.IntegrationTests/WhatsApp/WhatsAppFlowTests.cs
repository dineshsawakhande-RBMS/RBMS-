using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RBMS.Application.Common.Models;
using RBMS.Application.Features.Auth;
using RBMS.Application.Features.Inventory;
using RBMS.Application.Features.Inventory.Commands;
using RBMS.Application.Features.Sales;
using RBMS.Application.Features.Sales.Commands;
using RBMS.Application.Features.WhatsApp;
using RBMS.Domain.Enums;
using Xunit;

namespace RBMS.IntegrationTests.WhatsApp;

public class WhatsAppFlowTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public WhatsAppFlowTests(CustomWebApplicationFactory factory)
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

    [Fact]
    public async Task Sending_a_message_records_it_as_sent()
    {
        await AuthenticateAsync();
        var resp = await _client.PostAsJsonAsync("/api/whatsapp/messages", new
        {
            toPhone = "9876500000",
            recipientName = "Test Customer",
            kind = "Custom",
            body = "Your order is ready for pickup.",
            relatedEntityType = (string?)null,
            relatedEntityId = (Guid?)null,
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await _client.GetFromJsonAsync<PagedResult<WhatsAppMessageDto>>(
            "/api/whatsapp/messages", TestJson.Options);
        var msg = list!.Items.First();
        msg.Status.Should().Be(WhatsAppMessageStatus.Sent);
        msg.Provider.Should().Be("LocalStub");
        msg.ProviderMessageId.Should().NotBeNullOrEmpty();
        msg.SentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Sending_an_invoice_targets_the_sale_customer()
    {
        await AuthenticateAsync();
        var storeId = Guid.NewGuid();
        var variantId = CustomWebApplicationFactory.Seed.VariantId;
        await _client.PostAsJsonAsync("/api/inventory/adjustments",
            new AdjustStockCommand(storeId, "Opening", new[] { new AdjustStockLineInput(variantId, 20, 100m) }));

        var custResp = await _client.PostAsJsonAsync("/api/customers", new { name = "Meena", mobile = "9111122223" });
        var customerId = await custResp.Content.ReadFromJsonAsync<Guid>();

        var saleResp = await _client.PostAsJsonAsync("/api/sales", new CreateSaleCommand(
            storeId, customerId, 0,
            new[] { new SaleItemInput(variantId, 2, 300, 0, 12) },
            new[] { new SalePaymentInput(PaymentMethod.Cash, 672, null) }, null));
        var saleId = await saleResp.Content.ReadFromJsonAsync<Guid>();

        var send = await _client.PostAsync($"/api/whatsapp/sales/{saleId}/invoice", null);
        send.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await _client.GetFromJsonAsync<PagedResult<WhatsAppMessageDto>>(
            "/api/whatsapp/messages", TestJson.Options);
        var msg = list!.Items.First(m => m.RelatedEntityId == saleId);
        msg.Kind.Should().Be(WhatsAppMessageKind.Invoice);
        msg.ToPhone.Should().Be("9111122223");
        msg.Body.Should().Contain("Meena");
        msg.Status.Should().Be(WhatsAppMessageStatus.Sent);
    }

    [Fact]
    public async Task Invoice_for_walk_in_sale_without_customer_is_rejected()
    {
        await AuthenticateAsync();
        var storeId = Guid.NewGuid();
        var variantId = CustomWebApplicationFactory.Seed.VariantId;
        await _client.PostAsJsonAsync("/api/inventory/adjustments",
            new AdjustStockCommand(storeId, "Opening", new[] { new AdjustStockLineInput(variantId, 10, 100m) }));

        var saleResp = await _client.PostAsJsonAsync("/api/sales", new CreateSaleCommand(
            storeId, null, 0,
            new[] { new SaleItemInput(variantId, 1, 300, 0, 12) },
            new[] { new SalePaymentInput(PaymentMethod.Cash, 336, null) }, null));
        var saleId = await saleResp.Content.ReadFromJsonAsync<Guid>();

        var send = await _client.PostAsync($"/api/whatsapp/sales/{saleId}/invoice", null);
        send.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Empty_body_is_rejected()
    {
        await AuthenticateAsync();
        var resp = await _client.PostAsJsonAsync("/api/whatsapp/messages", new
        {
            toPhone = "9876500000", recipientName = (string?)null, kind = "Custom",
            body = "", relatedEntityType = (string?)null, relatedEntityId = (Guid?)null,
        });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
