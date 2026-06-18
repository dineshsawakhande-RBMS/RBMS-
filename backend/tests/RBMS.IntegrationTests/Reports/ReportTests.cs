using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RBMS.Application.Features.Auth;
using RBMS.Application.Features.Inventory;
using RBMS.Application.Features.Inventory.Commands;
using RBMS.Application.Features.Reports;
using RBMS.Application.Features.Sales;
using RBMS.Application.Features.Sales.Commands;
using RBMS.Domain.Enums;
using Xunit;

namespace RBMS.IntegrationTests.Reports;

public class ReportTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReportTests(CustomWebApplicationFactory factory)
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
    public async Task Sales_and_profit_reports_reflect_a_recorded_sale()
    {
        await AuthenticateAsync();
        var storeId = Guid.NewGuid();
        var variantId = CustomWebApplicationFactory.Seed.VariantId;

        // Stock in (cost 100), then sell 4 @ 250.
        await _client.PostAsJsonAsync("/api/inventory/adjustments",
            new AdjustStockCommand(storeId, "Opening", new[] { new AdjustStockLineInput(variantId, 20, 100m) }));
        await _client.PostAsJsonAsync("/api/sales", new CreateSaleCommand(
            storeId, null, 0,
            new[] { new SaleItemInput(variantId, 4, 250, 0, 12) },
            new[] { new SalePaymentInput(PaymentMethod.Cash, 1120, null) }, null));

        var sales = await _client.GetFromJsonAsync<SalesReportDto>("/api/reports/sales");
        sales!.Count.Should().BeGreaterThan(0);
        sales.TotalSales.Should().BeGreaterThan(0);

        var profit = await _client.GetFromJsonAsync<ProfitReportDto>("/api/reports/profit");
        // Revenue 1000 (4×250 taxable) − COGS 400 (4×100) = 600 for this product line.
        profit!.TotalRevenue.Should().BeGreaterThanOrEqualTo(1000m);
        profit.TotalProfit.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Sales_report_exports_as_csv()
    {
        await AuthenticateAsync();
        var resp = await _client.GetAsync("/api/reports/sales?format=csv");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var text = await resp.Content.ReadAsStringAsync();
        text.Should().Contain("Invoice");  // header row present
    }

    [Fact]
    public async Task Inventory_report_returns_valuation()
    {
        await AuthenticateAsync();
        var storeId = Guid.NewGuid();
        var variantId = CustomWebApplicationFactory.Seed.VariantId;
        await _client.PostAsJsonAsync("/api/inventory/adjustments",
            new AdjustStockCommand(storeId, "Opening", new[] { new AdjustStockLineInput(variantId, 10, 150m) }));

        var report = await _client.GetFromJsonAsync<InventoryReportDto>($"/api/reports/inventory?storeId={storeId}");
        report!.TotalValue.Should().Be(1500m); // 10 × 150
        report.LineCount.Should().Be(1);
    }
}
