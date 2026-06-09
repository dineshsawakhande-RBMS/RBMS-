using FluentValidation;
using MediatR;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;

namespace RBMS.Application.Features.Products.Commands;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Description,
    string? HsnCode,
    decimal GstRate,
    Guid? CategoryId,
    Guid? BrandId,
    bool IsActive) : IRequest, ITransactionalRequest;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.GstRate).InclusiveBetween(0, 100);
    }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand>
{
    private readonly IUnitOfWork _uow;

    public UpdateProductCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _uow.Repository<Product>().GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        product.Name = request.Name.Trim();
        product.Description = request.Description;
        product.HsnCode = request.HsnCode;
        product.GstRate = request.GstRate;
        product.CategoryId = request.CategoryId;
        product.BrandId = request.BrandId;
        product.IsActive = request.IsActive;

        _uow.Repository<Product>().Update(product);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
