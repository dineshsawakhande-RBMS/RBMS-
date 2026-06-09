using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Interfaces;
using RBMS.Domain.Common;

namespace RBMS.Infrastructure.Persistence.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    private readonly ApplicationDbContext _ctx;
    private readonly DbSet<T> _set;

    public GenericRepository(ApplicationDbContext ctx)
    {
        _ctx = ctx;
        _set = ctx.Set<T>();
    }

    // Respects global query filters (tenant + soft delete) unlike Find().
    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _set.FirstOrDefaultAsync(e => e.Id == id, ct);

    public IQueryable<T> Query() => _set;

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await _set.AddAsync(entity, ct);

    public void Update(T entity) => _set.Update(entity);

    public void Remove(T entity) => _set.Remove(entity); // soft-deleted via interceptor
}
