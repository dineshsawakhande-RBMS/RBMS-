using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RBMS.Application.Common.Models;
using RBMS.Application.Features.Auth;
using RBMS.Application.Features.Inventory;
using RBMS.Application.Features.Inventory.Commands;
using RBMS.Application.Features.Stores;
using RBMS.Application.Features.Stores.Commands;
using Xunit;

namespace RBMS.IntegrationTests.Stores;

public class StoreFlowTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public StoreFlowTests(CustomWebApplicationFactory factory)
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

    private async Task<Guid> CreateStoreAsync(string code, string name) =>
        await (await _client.PostAsJsonAsync("/api/stores", new CreateStoreCommand(
            code, name, null, null, null, null, "Pune", "MH", null)))
            .Content.ReadFromJsonAsync<Guid>();

    private async Task StockInAsync(Guid storeId, Guid variantId, decimal qty) =>
        (await _client.PostAsJsonAsync("/api/inventory/adjustments",
            new AdjustStockCommand(storeId, "Opening", new[] { new AdjustStockLineInput(variantId, qty, 100m) })))
        .EnsureSuccessStatusCode();

    private async Task<decimal> OnHandAsync(Guid storeId, Guid variantId)
    {
        var levels = await _client.GetFromJsonAsync<PagedResult<StockLevelDto>>(
            $"/api/inventory/levels?storeId={storeId}&pageSize=200");
        var row = levels!.Items.FirstOrDefault(i => i.VariantId == variantId);
        return row?.QuantityOnHand ?? 0;
    }

    [Fact]
    public async Task Create_list_and_fetch_store()
    {
        await AuthenticateAsync();
        var id = await CreateStoreAsync($"ST-{Guid.NewGuid():N}".Substring(0, 10), "Branch Two");

        var list = await _client.GetFromJsonAsync<List<StoreListItemDto>>("/api/stores");
        list!.Should().Contain(s => s.Id == id && s.Name == "Branch Two");

        var detail = await _client.GetFromJsonAsync<StoreDto>($"/api/stores/{id}");
        detail!.City.Should().Be("Pune");
        detail.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Duplicate_store_code_is_rejected()
    {
        await AuthenticateAsync();
        var code = $"ST-{Guid.NewGuid():N}".Substring(0, 10);
        await CreateStoreAsync(code, "First");
        var second = await _client.PostAsJsonAsync("/api/stores", new CreateStoreCommand(
            code, "Second", null, null, null, null, null, null, null));
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Transfer_moves_stock_between_stores()
    {
        await AuthenticateAsync();
        var destStoreId = await CreateStoreAsync($"ST-{Guid.NewGuid():N}".Substring(0, 10), "Dest");
        var srcStoreId = await CreateStoreAsync($"ST-{Guid.NewGuid():N}".Substring(0, 10), "Src");
        var variantId = CustomWebApplicationFactory.Seed.VariantId;

        await StockInAsync(srcStoreId, variantId, 30);

        var transfer = await _client.PostAsJsonAsync("/api/inventory/transfers", new TransferStockCommand(
            FromStoreId: srcStoreId, ToStoreId: destStoreId,
            Lines: new[] { new TransferStockLineInput(variantId, 12) }, Notes: "rebalance"));
        transfer.StatusCode.Should().Be(HttpStatusCode.OK);

        (await OnHandAsync(srcStoreId, variantId)).Should().Be(18);   // 30 − 12
        (await OnHandAsync(destStoreId, variantId)).Should().Be(12);  // 0 + 12
    }

    [Fact]
    public async Task Transfer_exceeding_source_stock_is_rejected()
    {
        await AuthenticateAsync();
        var destStoreId = await CreateStoreAsync($"ST-{Guid.NewGuid():N}".Substring(0, 10), "Dest2");
        var srcStoreId = await CreateStoreAsync($"ST-{Guid.NewGuid():N}".Substring(0, 10), "Src2");
        var variantId = CustomWebApplicationFactory.Seed.VariantId;
        await StockInAsync(srcStoreId, variantId, 5);

        var transfer = await _client.PostAsJsonAsync("/api/inventory/transfers", new TransferStockCommand(
            srcStoreId, destStoreId, new[] { new TransferStockLineInput(variantId, 20) }, null));
        transfer.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Transfer_to_same_store_is_rejected()
    {
        await AuthenticateAsync();
        var storeId = await CreateStoreAsync($"ST-{Guid.NewGuid():N}".Substring(0, 10), "Same");
        var variantId = CustomWebApplicationFactory.Seed.VariantId;

        var transfer = await _client.PostAsJsonAsync("/api/inventory/transfers", new TransferStockCommand(
            storeId, storeId, new[] { new TransferStockLineInput(variantId, 1) }, null));
        transfer.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
