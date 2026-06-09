namespace RBMS.Application.Common.Security;

/// <summary>Permission codes — must match the catalogue seeded in schema.sql.</summary>
public static class Permissions
{
    public const string DashboardView = "dashboard.view";
    public const string ProductView = "product.view";
    public const string ProductManage = "product.manage";
    public const string InventoryView = "inventory.view";
    public const string InventoryAdjust = "inventory.adjust";
    public const string PurchaseView = "purchase.view";
    public const string PurchaseManage = "purchase.manage";
    public const string SaleCreate = "sale.create";
    public const string SaleRefund = "sale.refund";
    public const string CustomerManage = "customer.manage";
    public const string SupplierManage = "supplier.manage";
    public const string EmployeeManage = "employee.manage";
    public const string PayrollManage = "payroll.manage";
    public const string ExpenseManage = "expense.manage";
    public const string DocumentView = "document.view";
    public const string DocumentManage = "document.manage";
    public const string ReportView = "report.view";
    public const string UserManage = "user.manage";
    public const string AuditView = "audit.view";
}
