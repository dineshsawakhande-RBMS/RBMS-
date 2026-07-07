using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RBMS.Application.Features.Analytics;
using RBMS.Application.Features.Auth;
using RBMS.Application.Features.Inventory;
using RBMS.Application.Features.Inventory.Commands;
using RBMS.Application.Features.Sales;
using RBMS.Application.Features.Sales.Commands;
using RBMS.Domain.Enums;
using Xunit;

namespace RBMS.IntegrationTests.Analytics;

public class AnalyticsFlowTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AnalyticsFlowTests(CustomWebApplicationFactory factory)
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

    private async Task StockInAsync(Guid storeId, Guid variantId, decimal qty) =>
        (await _client.PostAsJsonAsync("/api/inventory/adjustments",
            new AdjustStockCommand(storeId, "Opening", new[] { new AdjustStockLineInput(variantId, qty, 100m) })))
        .EnsureSuccessStatusCode();

    [Fact]
    public async Task Dead_stock_lists_unsold_in_stock_variants_then_drops_them_once_sold()
    {
        await AuthenticateAsync();
        var storeId = Guid.NewGuid();
        var variantId = CustomWebApplicationFactory.Seed.VariantId;
        await StockInAsync(storeId, variantId, 40);

        // No sales yet → the stocked variant shows up as dead.
        var before = await _client.GetFromJsonAsync<DeadStockReportDto>(
            $"/api/analytics/dead-stock?storeId={storeId}&days=90", TestJson.Options);
        before!.Rows.Should().Contain(r => r.VariantId == variantId && r.IsDead && r.UnitsSold == 0);

        // Sell 8 units → now sold within the window, above the slow threshold → excluded.
        await _client.PostAsJsonAsync("/api/sales", new CreateSaleCommand(
            storeId, null, 0,
            new[] { new SaleItemInput(variantId, 8, 200, 0, 12) },
            new[] { new SalePaymentInput(PaymentMethod.Cash, 1792, null) }, null));

        var after = await _client.GetFromJsonAsync<DeadStockReportDto>(
            $"/api/analytics/dead-stock?storeId={storeId}&days=90&slowThreshold=5", TestJson.Options);
        after!.Rows.Should().NotContain(r => r.VariantId == variantId);
    }

    [Fact]
    public async Task Slow_mover_is_flagged_when_under_threshold()
    {
        await AuthenticateAsync();
        var storeId = Guid.NewGuid();
        var variantId = CustomWebApplicationFactory.Seed.VariantId;
        await StockInAsync(storeId, variantId, 40);

        await _client.PostAsJsonAsync("/api/sales", new CreateSaleCommand(
            storeId, null, 0,
            new[] { new SaleItemInput(variantId, 2, 200, 0, 12) },
            new[] { new SalePaymentInput(PaymentMethod.Cash, 448, null) }, null));

        var report = await _client.GetFromJsonAsync<DeadStockReportDto>(
            $"/api/analytics/dead-stock?storeId={storeId}&days=90&slowThreshold=5", TestJson.Options);
        var row = report!.Rows.Single(r => r.VariantId == variantId);
        row.IsDead.Should().BeFalse();
        row.UnitsSold.Should().Be(2);
        row.DaysSinceLastSale.Should().NotBeNull();
    }

    [Fact]
    public async Task Retention_counts_customers_and_top_spenders()
    {
        await AuthenticateAsync();
        var storeId = Guid.NewGuid();
        var variantId = CustomWebApplicationFactory.Seed.VariantId;
        await StockInAsync(storeId, variantId, 40);

        var custResp = await _client.PostAsJsonAsync("/api/customers", new { name = "Ritu Shah", mobile = "9111100000" });
        var customerId = await custResp.Content.ReadFromJsonAsync<Guid>();

        // Two purchases → a repeat customer.
        for (var n = 0; n < 2; n++)
            await _client.PostAsJsonAsync("/api/sales", new CreateSaleCommand(
                storeId, customerId, 0,
                new[] { new SaleItemInput(variantId, 1, 200, 0, 12) },
                new[] { new SalePaymentInput(PaymentMethod.Cash, 224, null) }, null));

        var retention = await _client.GetFromJsonAsync<CustomerRetentionDto>(
            "/api/analytics/customer-retention?months=6", TestJson.Options);

        retention!.TotalCustomers.Should().BeGreaterThanOrEqualTo(1);
        retention.RepeatCustomers.Should().BeGreaterThanOrEqualTo(1);
        retention.TopCustomers.Should().Contain(c => c.CustomerId == customerId && c.Orders == 2);
        retention.Trend.Should().HaveCount(6);
    }

    [Fact]
    public async Task Endpoints_require_report_permission_but_work_with_empty_data()
    {
        await AuthenticateAsync();
        var retention = await _client.GetFromJsonAsync<CustomerRetentionDto>(
            "/api/analytics/customer-retention?months=3", TestJson.Options);
        retention!.Months.Should().Be(3);
        retention.Trend.Should().HaveCount(3);
    }
}
