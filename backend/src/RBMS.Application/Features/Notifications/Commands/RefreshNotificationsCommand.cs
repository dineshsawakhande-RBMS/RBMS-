using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Application.Common.Security;
using RBMS.Domain.Entities;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Notifications.Commands;

/// <summary>
/// Reconciles the current user's in-app notifications against the live feeds they're allowed to
/// see (low-stock, expiring documents, salary-due, pending leaves). Idempotent: new alerts are
/// added, resolved ones are cleared, and existing ones (incl. their read state) are preserved.
/// </summary>
public record RefreshNotificationsCommand : IRequest<RefreshNotificationsResult>, ITransactionalRequest;

public class RefreshNotificationsCommandHandler
    : IRequestHandler<RefreshNotificationsCommand, RefreshNotificationsResult>
{
    private const int ExpiryWindowDays = 30;
    private const int PerTypeCap = 100;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public RefreshNotificationsCommandHandler(IApplicationDbContext db, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    private sealed record Desired(
        NotificationType Type, NotificationSeverity Severity, string Title, string Message,
        string LinkPath, string DedupKey, string RelatedEntityType, Guid RelatedEntityId);

    public async Task<RefreshNotificationsResult> Handle(RefreshNotificationsCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenAccessException("No user context.");
        var tenantId = _currentUser.TenantId ?? throw new ForbiddenAccessException("No tenant context.");
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);

        var scannedTypes = new HashSet<NotificationType>();
        var desired = new List<Desired>();

        // --- Low stock ---
        if (_currentUser.HasPermission(Permissions.InventoryView))
        {
            scannedTypes.Add(NotificationType.LowStock);
            var lows = await _db.Inventory.AsNoTracking()
                .Where(i => i.QuantityOnHand <= i.Variant.ReorderLevel)
                .OrderBy(i => i.QuantityOnHand)
                .Take(PerTypeCap)
                .Select(i => new { i.VariantId, i.Variant.Sku, Name = i.Variant.Product.Name, i.QuantityOnHand, i.Variant.ReorderLevel })
                .ToListAsync(ct);
            foreach (var l in lows)
                desired.Add(new Desired(
                    NotificationType.LowStock, NotificationSeverity.Warning, "Low stock",
                    $"{l.Name} ({l.Sku}) — {l.QuantityOnHand} left (reorder at {l.ReorderLevel}).",
                    "/inventory", $"lowstock:{l.VariantId}", "ProductVariant", l.VariantId));
        }

        // --- Documents expiring soon ---
        if (_currentUser.HasPermission(Permissions.DocumentView))
        {
            scannedTypes.Add(NotificationType.DocumentExpiring);
            var cutoff = today.AddDays(ExpiryWindowDays);
            var docs = await _db.Documents.AsNoTracking()
                .Where(d => d.ExpiryDate != null && d.ExpiryDate <= cutoff)
                .OrderBy(d => d.ExpiryDate)
                .Take(PerTypeCap)
                .Select(d => new { d.Id, d.Title, d.ExpiryDate })
                .ToListAsync(ct);
            foreach (var d in docs)
                desired.Add(new Desired(
                    NotificationType.DocumentExpiring,
                    d.ExpiryDate < today ? NotificationSeverity.Critical : NotificationSeverity.Warning,
                    "Document expiring",
                    $"\"{d.Title}\" {(d.ExpiryDate < today ? "expired" : "expires")} on {d.ExpiryDate:yyyy-MM-dd}.",
                    "/documents", $"docexpiry:{d.Id}", "Document", d.Id));
        }

        // --- Salary due (no payroll for last month) ---
        if (_currentUser.HasPermission(Permissions.PayrollManage))
        {
            scannedTypes.Add(NotificationType.SalaryDue);
            var firstOfThisMonth = new DateOnly(today.Year, today.Month, 1);
            var prevMonthEnd = firstOfThisMonth.AddDays(-1);
            int year = prevMonthEnd.Year, month = prevMonthEnd.Month;

            var paidEmployeeIds = await _db.Payrolls.AsNoTracking()
                .Where(p => p.PeriodYear == year && p.PeriodMonth == month)
                .Select(p => p.EmployeeId)
                .ToListAsync(ct);

            var due = await _db.Employees.AsNoTracking()
                .Where(e => e.Status == EmploymentStatus.Active
                    && e.JoiningDate <= prevMonthEnd
                    && !paidEmployeeIds.Contains(e.Id))
                .OrderBy(e => e.FullName)
                .Take(PerTypeCap)
                .Select(e => new { e.Id, e.FullName })
                .ToListAsync(ct);

            foreach (var e in due)
                desired.Add(new Desired(
                    NotificationType.SalaryDue, NotificationSeverity.Info, "Salary due",
                    $"Payroll for {month:00}/{year} not generated for {e.FullName}.",
                    "/salary", $"salarydue:{e.Id}:{year}-{month:00}", "Employee", e.Id));
        }

        // --- Pending leave (for managers / responsible persons) ---
        if (_currentUser.HasPermission(Permissions.LeaveApprove))
        {
            scannedTypes.Add(NotificationType.LeavePending);
            var pending = await _db.Leaves.AsNoTracking()
                .Where(l => l.Status == LeaveStatus.Pending)
                .OrderBy(l => l.FromDate)
                .Take(PerTypeCap)
                .Select(l => new { l.Id, Name = l.Employee.FullName, l.LeaveType, l.FromDate, l.ToDate, l.Days })
                .ToListAsync(ct);
            foreach (var l in pending)
                desired.Add(new Desired(
                    NotificationType.LeavePending, NotificationSeverity.Info, "Leave approval pending",
                    $"{l.Name} requested {l.LeaveType} leave {l.FromDate:yyyy-MM-dd}–{l.ToDate:yyyy-MM-dd} ({l.Days}d).",
                    "/attendance", $"leavepending:{l.Id}", "LeaveRequest", l.Id));
        }

        // --- Reconcile (only the types we scanned) ---
        var existing = await _db.Notifications
            .Where(n => n.UserId == userId && scannedTypes.Contains(n.Type))
            .ToListAsync(ct);

        var desiredKeys = desired.Select(d => d.DedupKey).ToHashSet();
        var existingKeys = existing.Select(n => n.DedupKey).ToHashSet();

        var cleared = 0;
        foreach (var stale in existing.Where(n => !desiredKeys.Contains(n.DedupKey)))
        {
            _db.Notifications.Remove(stale);   // soft-delete; filtered unique index allows re-create later
            cleared++;
        }

        var created = 0;
        foreach (var d in desired.Where(d => !existingKeys.Contains(d.DedupKey)))
        {
            _db.Notifications.Add(new Notification
            {
                TenantId = tenantId,
                UserId = userId,
                Type = d.Type,
                Severity = d.Severity,
                Title = d.Title,
                Message = d.Message,
                LinkPath = d.LinkPath,
                DedupKey = d.DedupKey,
                RelatedEntityType = d.RelatedEntityType,
                RelatedEntityId = d.RelatedEntityId,
                IsRead = false,
            });
            created++;
        }

        if (created > 0 || cleared > 0) await _db.SaveChangesAsync(ct);

        var unread = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);
        return new RefreshNotificationsResult(created, cleared, unread);
    }
}
