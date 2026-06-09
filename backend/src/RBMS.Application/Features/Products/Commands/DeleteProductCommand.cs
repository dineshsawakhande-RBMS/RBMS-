using MediatR;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;

namespace RBMS.Application.Features.Products.Commands;

/// <summary>Soft-deletes a product. The interceptor flags it; it is never physically removed.</summary>
public record DeleteProductCommand(Guid Id) : IRequest, ITransactionalRequest;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteProductCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _uow.Repository<Product>().GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        _uow.Repository<Product>().Remove(product);   // → soft delete via interceptor
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
