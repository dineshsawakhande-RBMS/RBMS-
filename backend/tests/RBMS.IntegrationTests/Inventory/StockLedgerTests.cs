using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Services;
using RBMS.Domain.Enums;
using RBMS.Infrastructure.Persistence;
using Xunit;

namespace RBMS.IntegrationTests.Inventory;

/// <summary>
/// Fast tests for the stock ledger against a standalone in-memory context (no web host).
/// Verifies the "never touch stock directly" invariant: every change flows through the
/// ledger, the projection stays correct, moving-average cost is maintained, and stock can
/// never go negative.
/// </summary>
public class StockLedgerTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid Store = Guid.NewGuid();
    private static readonly Guid Variant = Guid.NewGuid();

    private static (ApplicationDbContext db, StockLedger ledger) NewLedger()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"ledger-{Guid.NewGuid()}")
            .Options;
        var currentUser = new TestCurrentUser(Tenant);
        var db = new ApplicationDbContext(options, currentUser);
        var ledger = new StockLedger(db, currentUser, new FixedClock());
        return (db, ledger);
    }

    [Fact]
    public async Task Inbound_movement_increases_quantity_and_writes_a_movement()
    {
        var (db, ledger) = NewLedger();

        await ledger.ApplyAsync(Store, Variant, StockMovementType.PurchaseIn, 10, unitCost: 100);
        await db.SaveChangesAsync();

        var inv = await db.Inventory.SingleAsync();
        inv.QuantityOnHand.Should().Be(10);
        inv.AvgCost.Should().Be(100);

        var move = await db.StockMovements.SingleAsync();
        move.Quantity.Should().Be(10);          // signed +
        move.BalanceAfter.Should().Be(10);
        move.MovementType.Should().Be(StockMovementType.PurchaseIn);
    }

    [Fact]
    public async Task Moving_average_cost_is_recomputed_on_second_inbound()
    {
        var (db, ledger) = NewLedger();

        await ledger.ApplyAsync(Store, Variant, StockMovementType.PurchaseIn, 10, unitCost: 100);
        await ledger.ApplyAsync(Store, Variant, StockMovementType.PurchaseIn, 10, unitCost: 200);
        await db.SaveChangesAsync();

        var inv = await db.Inventory.SingleAsync();
        inv.QuantityOnHand.Should().Be(20);
        inv.AvgCost.Should().Be(150);           // (10*100 + 10*200) / 20
    }

    [Fact]
    public async Task Outbound_movement_decreases_quantity_with_negative_signed_quantity()
    {
        var (db, ledger) = NewLedger();

        await ledger.ApplyAsync(Store, Variant, StockMovementType.PurchaseIn, 10, unitCost: 100);
        await ledger.ApplyAsync(Store, Variant, StockMovementType.SaleOut, 4);
        await db.SaveChangesAsync();

        var inv = await db.Inventory.SingleAsync();
        inv.QuantityOnHand.Should().Be(6);

        var outMove = await db.StockMovements.SingleAsync(m => m.MovementType == StockMovementType.SaleOut);
        outMove.Quantity.Should().Be(-4);        // signed −
        outMove.BalanceAfter.Should().Be(6);
    }

    [Fact]
    public async Task Outbound_beyond_available_throws_and_does_not_go_negative()
    {
        var (db, ledger) = NewLedger();
        await ledger.ApplyAsync(Store, Variant, StockMovementType.PurchaseIn, 5, unitCost: 100);
        await db.SaveChangesAsync();

        var act = async () =>
        {
            await ledger.ApplyAsync(Store, Variant, StockMovementType.SaleOut, 99);
            await db.SaveChangesAsync();
        };

        await act.Should().ThrowAsync<ConflictException>();
    }

    private sealed class FixedClock : IDateTime
    {
        public DateTimeOffset UtcNow => new(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public TestCurrentUser(Guid tenantId) => TenantId = tenantId;
        public Guid? UserId => Guid.Parse("99999999-9999-9999-9999-999999999999");
        public Guid? TenantId { get; }
        public Guid? StoreId => null;
        public string? Username => "test";
        public string? IpAddress => null;
        public IReadOnlyCollection<string> Roles => Array.Empty<string>();
        public IReadOnlyCollection<string> Permissions => Array.Empty<string>();
        public bool IsAuthenticated => true;
        public bool HasPermission(string permission) => true;
    }
}
