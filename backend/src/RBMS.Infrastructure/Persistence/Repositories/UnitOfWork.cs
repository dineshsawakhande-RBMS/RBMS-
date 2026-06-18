using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Interfaces;
using RBMS.Domain.Common;

namespace RBMS.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _ctx;
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(ApplicationDbContext ctx) => _ctx = ctx;

    public IGenericRepository<T> Repository<T>() where T : BaseEntity
    {
        if (_repositories.TryGetValue(typeof(T), out var existing))
            return (IGenericRepository<T>)existing;

        var repo = new GenericRepository<T>(_ctx);
        _repositories[typeof(T)] = repo;
        return repo;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _ctx.SaveChangesAsync(ct);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct = default)
    {
        // Wrap begin→work→save→commit in the execution strategy so it is a single retriable
        // unit. Required because the context enables retry-on-failure; the strategy may run
        // the whole delegate more than once, so the operation must be idempotent within it.
        var strategy = _ctx.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async token =>
        {
            await using var transaction = await _ctx.Database.BeginTransactionAsync(token);
            var result = await operation();
            await _ctx.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            return result;
        }, ct);
    }
}
