using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RBMS.Application.Common.Models;
using RBMS.Application.Features.Auth;
using RBMS.Application.Features.Inventory;
using RBMS.Application.Features.Purchases;
using RBMS.Application.Features.Purchases.Commands;
using RBMS.Application.Features.Suppliers;
using RBMS.Application.Features.Suppliers.Commands;
using Xunit;

namespace RBMS.IntegrationTests.Purchases;

public class PurchaseFlowTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PurchaseFlowTests(CustomWebApplicationFactory factory)
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

    private async Task<Guid> CreateSupplierAsync()
    {
        var resp = await _client.PostAsJsonAsync("/api/suppliers", new CreateSupplierCommand(
            Code: $"SUP-{Guid.NewGuid():N}".Substring(0, 12), Name: "Fabric House", Gstin: null,
            ContactPerson: "Ramesh", Phone: "9999999999", Email: null, AddressLine1: null,
            City: "Surat", State: "Gujarat", Pincode: null, PaymentTermsDays: 30, OpeningBalance: 0));
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return await resp.Content.ReadFromJsonAsync<Guid>();
    }

    [Fact]
    public async Task Goods_receipt_increases_stock_and_supplier_balance()
    {
        await AuthenticateAsync();
        var supplierId = await CreateSupplierAsync();
        var storeId = Guid.NewGuid();                       // isolated inventory
        var variantId = CustomWebApplicationFactory.Seed.VariantId;

        // Purchase 10 @ 100 with 12% GST → subtotal 1000, tax 120, grand total 1120.
        var create = await _client.PostAsJsonAsync("/api/purchases", new CreatePurchaseCommand(
            supplierId, storeId, InvoiceNumber: "INV-001", InvoiceDate: new DateOnly(2026, 6, 18),
            Discount: 0, AmountPaid: 0, Notes: null,
            Items: new[] { new PurchaseItemInput(variantId, 10, 100, 12) }));
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        // Stock came IN via the ledger.
        var levels = await _client.GetFromJsonAsync<PagedResult<StockLevelDto>>(
            $"/api/inventory/levels?storeId={storeId}");
        levels!.Items.Single(i => i.VariantId == variantId).QuantityOnHand.Should().Be(10);

        // Supplier now owes the grand total.
        var supplier = await _client.GetFromJsonAsync<SupplierDto>($"/api/suppliers/{supplierId}");
        supplier!.OutstandingBalance.Should().Be(1120m);
    }

    [Fact]
    public async Task Paying_part_of_a_purchase_reduces_the_outstanding_balance()
    {
        await AuthenticateAsync();
        var supplierId = await CreateSupplierAsync();
        var storeId = Guid.NewGuid();
        var variantId = CustomWebApplicationFactory.Seed.VariantId;

        await _client.PostAsJsonAsync("/api/purchases", new CreatePurchaseCommand(
            supplierId, storeId, "INV-002", new DateOnly(2026, 6, 18),
            Discount: 0, AmountPaid: 500, Notes: null,
            Items: new[] { new PurchaseItemInput(variantId, 10, 100, 12) }));

        var supplier = await _client.GetFromJsonAsync<SupplierDto>($"/api/suppliers/{supplierId}");
        supplier!.OutstandingBalance.Should().Be(620m);    // 1120 owed − 500 paid
    }

    [Fact]
    public async Task Supplier_ledger_reflects_a_purchase()
    {
        await AuthenticateAsync();
        var supplierId = await CreateSupplierAsync();
        var storeId = Guid.NewGuid();
        var variantId = CustomWebApplicationFactory.Seed.VariantId;

        await _client.PostAsJsonAsync("/api/purchases", new CreatePurchaseCommand(
            supplierId, storeId, "INV-L1", new DateOnly(2026, 6, 18),
            Discount: 0, AmountPaid: 0, Notes: null,
            Items: new[] { new PurchaseItemInput(variantId, 10, 100, 12) }));

        var ledger = await _client.GetFromJsonAsync<SupplierLedgerDto>($"/api/suppliers/{supplierId}/ledger");
        ledger!.Entries.Should().NotBeEmpty();
        ledger.Outstanding.Should().Be(1120m);
        ledger.Entries[^1].RunningBalance.Should().Be(1120m);
    }

    [Fact]
    public async Task Purchase_return_reduces_stock_and_supplier_balance()
    {
        await AuthenticateAsync();
        var supplierId = await CreateSupplierAsync();
        var storeId = Guid.NewGuid();
        var variantId = CustomWebApplicationFactory.Seed.VariantId;

        await _client.PostAsJsonAsync("/api/purchases", new CreatePurchaseCommand(
            supplierId, storeId, "INV-003", new DateOnly(2026, 6, 18),
            Discount: 0, AmountPaid: 0, Notes: null,
            Items: new[] { new PurchaseItemInput(variantId, 10, 100, 12) }));

        var ret = await _client.PostAsJsonAsync("/api/purchases/returns", new CreatePurchaseReturnCommand(
            supplierId, storeId, PurchaseId: null, Reason: "Defective",
            Items: new[] { new PurchaseReturnItemInput(variantId, 4, 100) }));
        ret.StatusCode.Should().Be(HttpStatusCode.OK);

        var levels = await _client.GetFromJsonAsync<PagedResult<StockLevelDto>>(
            $"/api/inventory/levels?storeId={storeId}");
        levels!.Items.Single(i => i.VariantId == variantId).QuantityOnHand.Should().Be(6);  // 10 − 4

        var supplier = await _client.GetFromJsonAsync<SupplierDto>($"/api/suppliers/{supplierId}");
        supplier!.OutstandingBalance.Should().Be(720m);    // 1120 − 400 returned
    }
}
