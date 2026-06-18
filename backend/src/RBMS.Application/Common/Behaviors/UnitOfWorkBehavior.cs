using MediatR;
using Microsoft.Extensions.Logging;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;

namespace RBMS.Application.Common.Behaviors;

/// <summary>
/// Wraps requests marked <see cref="ITransactionalRequest"/> (commands) in a database
/// transaction: commit on success, rollback on any exception. Queries are untouched.
/// </summary>
public class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UnitOfWorkBehavior<TRequest, TResponse>> _logger;

    public UnitOfWorkBehavior(IUnitOfWork uow, ILogger<UnitOfWorkBehavior<TRequest, TResponse>> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ITransactionalRequest)
            return await next();

        // The unit of work runs this inside a retry-safe transaction (commit on success,
        // rollback on exception). Logging the rollback is left to the failing handler's log.
        return await _uow.ExecuteInTransactionAsync(() => next(), cancellationToken);
    }
}
