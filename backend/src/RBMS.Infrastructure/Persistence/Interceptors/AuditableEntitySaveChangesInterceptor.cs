using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RBMS.Application.Common.Interfaces;
using RBMS.Domain.Common;
using RBMS.Domain.Entities;
using RBMS.Domain.Enums;

namespace RBMS.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Enforces three cross-cutting rules at the persistence boundary so no handler can forget
/// them: (1) stamps audit columns, (2) converts hard deletes of soft-deletable entities
/// into soft deletes, (3) writes an <see cref="AuditLog"/> row with old/new values.
/// </summary>
public class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public AuditableEntitySaveChangesInterceptor(ICurrentUser currentUser, IDateTime clock)
    {
        _currentUser = currentUser;
        _clock = clock;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private void Apply(DbContext? context)
    {
        if (context is null) return;

        var now = _clock.UtcNow;
        var userId = _currentUser.UserId;
        var tenantId = _currentUser.TenantId;
        var auditLogs = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is AuditLog or LoginHistory) continue; // never audit the audit trail

            // ---- 1 & 2: stamp + soft-delete conversion ----
            if (entry.Entity is IAuditableEntity auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditable.CreatedAt = now;
                        auditable.CreatedBy = userId;
                        if (entry.Entity is ITenantEntity te && te.TenantId == Guid.Empty && tenantId is { } t)
                            te.TenantId = t;
                        break;
                    case EntityState.Modified:
                        auditable.UpdatedAt = now;
                        auditable.UpdatedBy = userId;
                        break;
                }
            }

            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable soft)
            {
                entry.State = EntityState.Modified;
                soft.IsDeleted = true;
                soft.DeletedAt = now;
                soft.DeletedBy = userId;
            }

            // ---- 3: audit log ----
            var action = entry.State switch
            {
                EntityState.Added => (AuditAction?)AuditAction.Create,
                EntityState.Modified => entry.Entity is ISoftDeletable { IsDeleted: true }
                    ? AuditAction.SoftDelete : AuditAction.Update,
                EntityState.Deleted => AuditAction.Delete,
                _ => null
            };
            if (action is null) continue;

            auditLogs.Add(BuildAuditLog(entry, action.Value, userId, tenantId, now));
        }

        foreach (var log in auditLogs)
            context.Set<AuditLog>().Add(log);
    }

    private AuditLog BuildAuditLog(
        EntityEntry entry, AuditAction action, Guid? userId, Guid? tenantId, DateTimeOffset now)
    {
        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();
        var changed = new List<string>();

        foreach (var prop in entry.Properties)
        {
            var name = prop.Metadata.Name;
            if (name is "Version") continue;

            switch (action)
            {
                case AuditAction.Create:
                    newValues[name] = prop.CurrentValue;
                    break;
                case AuditAction.Delete:
                    oldValues[name] = prop.OriginalValue;
                    break;
                default: // Update / SoftDelete
                    if (prop.IsModified && !Equals(prop.OriginalValue, prop.CurrentValue))
                    {
                        oldValues[name] = prop.OriginalValue;
                        newValues[name] = prop.CurrentValue;
                        changed.Add(name);
                    }
                    break;
            }
        }

        var tenant = (entry.Entity as ITenantEntity)?.TenantId ?? tenantId;

        return new AuditLog
        {
            TenantId = tenant,
            UserId = userId,
            Action = action,
            EntityName = entry.Entity.GetType().Name,
            EntityId = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "",
            OldValues = oldValues.Count == 0 ? null : JsonSerializer.Serialize(oldValues),
            NewValues = newValues.Count == 0 ? null : JsonSerializer.Serialize(newValues),
            ChangedColumns = changed.Count == 0 ? null : changed.ToArray(),
            IpAddress = _currentUser.IpAddress,
            CreatedAt = now
        };
    }
}
