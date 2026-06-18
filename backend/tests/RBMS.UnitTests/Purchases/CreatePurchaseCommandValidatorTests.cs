using FluentAssertions;
using FluentValidation.TestHelper;
using RBMS.Application.Features.Purchases;
using RBMS.Application.Features.Purchases.Commands;
using Xunit;

namespace RBMS.UnitTests.Purchases;

public class CreatePurchaseCommandValidatorTests
{
    private readonly CreatePurchaseCommandValidator _validator = new();

    private static CreatePurchaseCommand Valid(params PurchaseItemInput[] items) => new(
        SupplierId: Guid.NewGuid(),
        StoreId: Guid.NewGuid(),
        InvoiceNumber: "INV-1",
        InvoiceDate: new DateOnly(2026, 6, 18),
        Discount: 0,
        AmountPaid: 0,
        Notes: null,
        Items: items.Length == 0 ? new[] { new PurchaseItemInput(Guid.NewGuid(), 5, 100, 12) } : items);

    [Fact]
    public void Valid_command_passes()
        => _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void No_items_fails()
        => _validator.TestValidate(Valid() with { Items = Array.Empty<PurchaseItemInput>() })
            .ShouldHaveValidationErrorFor(c => c.Items);

    [Fact]
    public void Empty_supplier_fails()
        => _validator.TestValidate(Valid() with { SupplierId = Guid.Empty })
            .ShouldHaveValidationErrorFor(c => c.SupplierId);

    [Fact]
    public void Zero_quantity_line_fails()
    {
        var cmd = Valid(new PurchaseItemInput(Guid.NewGuid(), 0, 100, 12));
        _validator.TestValidate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Negative_discount_fails()
        => _validator.TestValidate(Valid() with { Discount = -1 })
            .ShouldHaveValidationErrorFor(c => c.Discount);
}
