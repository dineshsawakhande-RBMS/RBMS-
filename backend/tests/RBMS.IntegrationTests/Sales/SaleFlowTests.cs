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
using RBMS.Domain.Enums;
using Xunit;

namespace RBMS.IntegrationTests.Sales;

public class SaleFlowTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SaleFlowTests(CustomWebApplicationFactory factory)
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

    /// <summary>Stock a unique store via an inventory adjustment so the sale has stock to draw down.</summary>
    private async Task StockInAsync(Guid storeId, Guid variantId, decimal qty)
    {
        var resp = await _client.PostAsJsonAsync("/api/inventory/adjustments",
            new AdjustStockCommand(storeId, "Opening", new[] { new AdjustStockLineInput(variantId, qty, 100m) }));
        resp.EnsureSuccessStatusCode();
    }

    private async Task<decimal> OnHandAsync(Guid storeId, Guid variantId)
    {
        var levels = await _client.GetFromJsonAsync<PagedResult<StockLevelDto>>(
            $"/api/inventory/levels?storeId={storeId}&pageSize=200");
        return levels!.Items.Single(i => i.VariantId == variantId).QuantityOnHand;
    }

    [Fact]
    public async Task Selling_reduces_stock_and_records_a_paid_sale()
    {
        await AuthenticateAsync();
        var storeId = Guid.NewGuid();
        var variantId = CustomWebApplicationFactory.Seed.VariantId;
        await StockInAsync(storeId, variantId, 50);

        // Sell 5 @ 200 + 12% GST. Pay cash in full.
        var sale = await _client.PostAsJsonAsync("/api/sales", new CreateSaleCommand(
            StoreId: storeId, CustomerId: null, Discount: 0,
            Items: new[] { new SaleItemInput(variantId, 5, 200, 0, 12) },
            Payments: new[] { new SalePaymentInput(PaymentMethod.Cash, 1120, null) },
            Notes: null));
        sale.StatusCode.Should().Be(HttpStatusCode.Created);

        (await OnHandAsync(storeId, variantId)).Should().Be(45); // 50 − 5
    }

    [Fact]
    public async Task Overselling_is_rejected_with_conflict()
    {
        await AuthenticateAsync();
        var storeId = Guid.NewGuid();
        var variantId = CustomWebApplicationFactory.Seed.VariantId;
        await StockInAsync(storeId, variantId, 3);

        var sale = await _client.PostAsJsonAsync("/api/sales", new CreateSaleCommand(
            storeId, null, 0,
            new[] { new SaleItemInput(variantId, 10, 200, 0, 12) },
            new[] { new SalePaymentInput(PaymentMethod.Cash, 2240, null) },
            null));

        sale.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Sale_return_restores_stock()
    {
        await AuthenticateAsync();
        var storeId = Guid.NewGuid();
        var variantId = CustomWebApplicationFactory.Seed.VariantId;
        await StockInAsync(storeId, variantId, 20);

        var saleResp = await _client.PostAsJsonAsync("/api/sales", new CreateSaleCommand(
            storeId, null, 0,
            new[] { new SaleItemInput(variantId, 8, 200, 0, 12) },
            new[] { new SalePaymentInput(PaymentMethod.Card, 1792, "xxxx-1234") },
            null));
        var saleId = await saleResp.Content.ReadFromJsonAsync<Guid>();
        (await OnHandAsync(storeId, variantId)).Should().Be(12); // 20 − 8

        var ret = await _client.PostAsJsonAsync("/api/sales/returns", new CreateSaleReturnCommand(
            SaleId: saleId, StoreId: storeId, Reason: "Wrong size", RefundMethod: PaymentMethod.Cash,
            Items: new[] { new SaleReturnItemInput(variantId, 3, 200) }));
        ret.StatusCode.Should().Be(HttpStatusCode.OK);

        (await OnHandAsync(storeId, variantId)).Should().Be(15); // 12 + 3
    }
}
