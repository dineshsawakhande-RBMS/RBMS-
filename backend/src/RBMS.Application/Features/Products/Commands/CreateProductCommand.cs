using FluentValidation;
using MediatR;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;

namespace RBMS.Application.Features.Products.Commands;

public record CreateProductCommand(
    string Name,
    string? Description,
    string? HsnCode,
    decimal GstRate,
    Guid? CategoryId,
    Guid? BrandId,
    IReadOnlyList<CreateVariantInput> Variants) : IRequest<Guid>, ITransactionalRequest;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.GstRate).InclusiveBetween(0, 100);
        RuleFor(x => x.HsnCode).MaximumLength(10);
        RuleFor(x => x.Variants).NotEmpty().WithMessage("A product needs at least one variant.");
        RuleForEach(x => x.Variants).ChildRules(v =>
        {
            v.RuleFor(i => i.Sku).NotEmpty().MaximumLength(50);
            v.RuleFor(i => i.SellingPrice).GreaterThanOrEqualTo(0);
            v.RuleFor(i => i.PurchasePrice).GreaterThanOrEqualTo(0);
            v.RuleFor(i => i.ReorderLevel).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.Variants)
            .Must(v => v.Select(i => i.Sku).Distinct(StringComparer.OrdinalIgnoreCase).Count() == v.Count)
            .WithMessage("Variant SKUs must be unique within the product.");
    }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public CreateProductCommandHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId
            ?? throw new ForbiddenAccessException("No tenant context.");

        // SKU uniqueness within tenant (DB also enforces a unique index as a backstop).
        var variants = _uow.Repository<ProductVariant>();
        foreach (var sku in request.Variants.Select(v => v.Sku))
        {
            var exists = variants.Query().Any(v => v.Sku == sku);
            if (exists)
                throw new ConflictException($"SKU '{sku}' already exists.");
        }

        var product = new Product
        {
            TenantId = tenantId,
            Name = request.Name.Trim(),
            Description = request.Description,
            HsnCode = request.HsnCode,
            GstRate = request.GstRate,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            IsActive = true,
            Variants = request.Variants.Select(v => new ProductVariant
            {
                TenantId = tenantId,
                Sku = v.Sku.Trim(),
                Barcode = v.Barcode,
                Size = v.Size,
                Color = v.Color,
                PurchasePrice = v.PurchasePrice,
                SellingPrice = v.SellingPrice,
                Mrp = v.Mrp,
                ReorderLevel = v.ReorderLevel,
                IsActive = true
            }).ToList()
        };

        await _uow.Repository<Product>().AddAsync(product, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return product.Id;
    }
}
