using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RBMS.Application.Common.Models;
using RBMS.Application.Features.Auth;
using RBMS.Application.Features.Inventory;
using RBMS.Application.Features.Inventory.Commands;
using Xunit;

namespace RBMS.IntegrationTests.Inventory;

public class InventoryFlowTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public InventoryFlowTests(CustomWebApplicationFactory factory)
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
    public async Task Adjusting_stock_updates_the_projected_levels_through_the_ledger()
    {
        await AuthenticateAsync();
        // Unique store per test → independent inventory row (keyed by store+variant),
        // so the shared in-memory DB doesn't leak counts between tests.
        var storeId = Guid.NewGuid();
        var variantId = CustomWebApplicationFactory.Seed.VariantId;

        // Stock in 10 via a manual adjustment.
        var adjust = await _client.PostAsJsonAsync("/api/inventory/adjustments",
            new AdjustStockCommand(storeId, "Opening count",
                new[] { new AdjustStockLineInput(variantId, 10, 500m) }));
        adjust.StatusCode.Should().Be(HttpStatusCode.OK);

        // Levels now show 10 on hand.
        var levels = await _client.GetFromJsonAsync<PagedResult<StockLevelDto>>(
            $"/api/inventory/levels?storeId={storeId}");
        var line = levels!.Items.Single(i => i.VariantId == variantId);
        line.QuantityOnHand.Should().Be(10);
        line.IsLow.Should().BeFalse();
    }

    [Fact]
    public async Task Recording_damaged_stock_reduces_quantity()
    {
        await AuthenticateAsync();
        var storeId = Guid.NewGuid();
        var variantId = CustomWebApplicationFactory.Seed.VariantId;

        await _client.PostAsJsonAsync("/api/inventory/adjustments",
            new AdjustStockCommand(storeId, "Opening count",
                new[] { new AdjustStockLineInput(variantId, 20, 500m) }));

        var damaged = await _client.PostAsJsonAsync("/api/inventory/damaged",
            new RecordDamagedStockCommand(storeId, variantId, 5, "Water damage"));
        damaged.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var levels = await _client.GetFromJsonAsync<PagedResult<StockLevelDto>>(
            $"/api/inventory/levels?storeId={storeId}");
        levels!.Items.Single(i => i.VariantId == variantId).QuantityOnHand.Should().Be(15);
    }

    [Fact]
    public async Task Removing_more_than_available_is_rejected_with_conflict()
    {
        await AuthenticateAsync();
        var storeId = Guid.NewGuid();
        var variantId = CustomWebApplicationFactory.Seed.VariantId;

        var response = await _client.PostAsJsonAsync("/api/inventory/damaged",
            new RecordDamagedStockCommand(storeId, variantId, 999_999, "impossible"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
