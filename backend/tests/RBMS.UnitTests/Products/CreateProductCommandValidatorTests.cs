using FluentAssertions;
using FluentValidation.TestHelper;
using RBMS.Application.Features.Products;
using RBMS.Application.Features.Products.Commands;
using Xunit;

namespace RBMS.UnitTests.Products;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    private static CreateProductCommand Valid(params CreateVariantInput[] variants) => new(
        Name: "Floral Maxi Dress",
        Description: "Summer collection",
        HsnCode: "6204",
        GstRate: 12m,
        CategoryId: null,
        BrandId: null,
        Variants: variants.Length == 0
            ? new[] { new CreateVariantInput("SKU-1", "BC1", "M", "Red", 500, 999, 1299, 3) }
            : variants);

    [Fact]
    public void Valid_command_passes()
        => _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Empty_name_fails()
        => _validator.TestValidate(Valid() with { Name = "" })
            .ShouldHaveValidationErrorFor(c => c.Name);

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Out_of_range_gst_fails(decimal gst)
        => _validator.TestValidate(Valid() with { GstRate = gst })
            .ShouldHaveValidationErrorFor(c => c.GstRate);

    [Fact]
    public void No_variants_fails()
        => _validator.TestValidate(Valid() with { Variants = Array.Empty<CreateVariantInput>() })
            .ShouldHaveValidationErrorFor(c => c.Variants);

    [Fact]
    public void Duplicate_skus_fail()
    {
        var cmd = Valid(
            new CreateVariantInput("DUP", null, "S", "Blue", 100, 200, null, 1),
            new CreateVariantInput("dup", null, "M", "Blue", 100, 200, null, 1));

        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Variants);
    }

    [Fact]
    public void Negative_selling_price_fails()
    {
        var cmd = Valid(new CreateVariantInput("SKU-9", null, "L", "Black", 100, -5, null, 1));
        var result = _validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
    }
}
