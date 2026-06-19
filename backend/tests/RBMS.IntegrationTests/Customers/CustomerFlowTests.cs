using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RBMS.Application.Features.Auth;
using RBMS.Application.Features.Customers;
using RBMS.Application.Features.Customers.Commands;
using RBMS.Application.Features.Inventory;
using RBMS.Application.Features.Inventory.Commands;
using RBMS.Application.Features.Sales;
using RBMS.Application.Features.Sales.Commands;
using RBMS.Domain.Enums;
using Xunit;

namespace RBMS.IntegrationTests.Customers;

public class CustomerFlowTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CustomerFlowTests(CustomWebApplicationFactory factory)
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

    private async Task<Guid> CreateCustomerAsync(string mobile)
    {
        var resp = await _client.PostAsJsonAsync("/api/customers", new CreateCustomerCommand(
            Name: "Asha Verma", Mobile: mobile, Email: null, AddressLine1: null,
            City: "Pune", State: "MH", Pincode: null, Birthday: null, Anniversary: null));
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return await resp.Content.ReadFromJsonAsync<Guid>();
    }

    [Fact]
    public async Task Create_then_list_customer()
    {
        await AuthenticateAsync();
        var id = await CreateCustomerAsync($"9{Guid.NewGuid().ToString("N").Substring(0, 9)}");
        var detail = await _client.GetFromJsonAsync<CustomerDto>($"/api/customers/{id}");
        detail!.Name.Should().Be("Asha Verma");
        detail.LoyaltyPoints.Should().Be(0);
    }

    [Fact]
    public async Task Duplicate_mobile_is_rejected()
    {
        await AuthenticateAsync();
        var mobile = $"9{Guid.NewGuid().ToString("N").Substring(0, 9)}";
        await CreateCustomerAsync(mobile);
        var second = await _client.PostAsJsonAsync("/api/customers", new CreateCustomerCommand(
            "Other", mobile, null, null, null, null, null, null, null));
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Sale_with_customer_awards_loyalty_points()
    {
        await AuthenticateAsync();
        var storeId = Guid.NewGuid();
        var variantId = CustomWebApplicationFactory.Seed.VariantId;
        var customerId = await CreateCustomerAsync($"9{Guid.NewGuid().ToString("N").Substring(0, 9)}");

        await _client.PostAsJsonAsync("/api/inventory/adjustments",
            new AdjustStockCommand(storeId, "Opening", new[] { new AdjustStockLineInput(variantId, 20, 100m) }));

        // Sell for a grand total around 1120 → 11 points (1 per ₹100).
        await _client.PostAsJsonAsync("/api/sales", new CreateSaleCommand(
            storeId, customerId, 0,
            new[] { new SaleItemInput(variantId, 5, 200, 0, 12) },
            new[] { new SalePaymentInput(PaymentMethod.Cash, 1120, null) }, null));

        var customer = await _client.GetFromJsonAsync<CustomerDto>($"/api/customers/{customerId}");
        customer!.LoyaltyPoints.Should().BeGreaterThan(0);
    }
}
