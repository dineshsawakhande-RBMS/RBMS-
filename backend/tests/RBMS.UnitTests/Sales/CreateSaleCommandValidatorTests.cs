using FluentAssertions;
using FluentValidation.TestHelper;
using RBMS.Application.Features.Sales;
using RBMS.Application.Features.Sales.Commands;
using RBMS.Domain.Enums;
using Xunit;

namespace RBMS.UnitTests.Sales;

public class CreateSaleCommandValidatorTests
{
    private readonly CreateSaleCommandValidator _validator = new();

    private static CreateSaleCommand Valid(params SaleItemInput[] items) => new(
        StoreId: Guid.NewGuid(),
        CustomerId: null,
        Discount: 0,
        Items: items.Length == 0 ? new[] { new SaleItemInput(Guid.NewGuid(), 2, 200, 0, 12) } : items,
        Payments: new[] { new SalePaymentInput(PaymentMethod.Cash, 448, null) },
        Notes: null);

    [Fact]
    public void Valid_command_passes()
        => _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void No_items_fails()
        => _validator.TestValidate(Valid() with { Items = Array.Empty<SaleItemInput>() })
            .ShouldHaveValidationErrorFor(c => c.Items);

    [Fact]
    public void Zero_quantity_line_fails()
        => _validator.TestValidate(Valid(new SaleItemInput(Guid.NewGuid(), 0, 200, 0, 12)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Gst_over_100_fails()
        => _validator.TestValidate(Valid(new SaleItemInput(Guid.NewGuid(), 1, 200, 0, 120)))
            .IsValid.Should().BeFalse();
}
