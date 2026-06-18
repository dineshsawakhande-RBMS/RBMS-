using RBMS.Domain.Common;

namespace RBMS.Application.Common.Interfaces;

/// <summary>Generic write-side repository over an aggregate/entity.</summary>
public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    IQueryable<T> Query();
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    /// <summary>Marks for removal. Soft-deletable entities are flagged, not physically deleted.</summary>
    void Remove(T entity);
}

/// <summary>
/// Coordinates repositories over a single DbContext and commits atomically.
/// </summary>
public interface IUnitOfWork
{
    IGenericRepository<T> Repository<T>() where T : BaseEntity;
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside a database transaction, wrapped in the
    /// provider's execution strategy so it is retry-safe (required when retry-on-failure is
    /// enabled). Commits on success; rolls back on any exception. Used by the transactional
    /// pipeline behavior for commands.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct = default);
}
