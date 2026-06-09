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
/// Coordinates repositories over a single transaction/DbContext and commits atomically.
/// The transactional pipeline behavior calls <see cref="SaveChangesAsync"/> for commands.
/// </summary>
public interface IUnitOfWork
{
    IGenericRepository<T> Repository<T>() where T : BaseEntity;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
