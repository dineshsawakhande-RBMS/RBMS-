namespace RBMS.Domain.Enums;

// Mirrors the native enum types in docs/database/schema.sql. Only a representative set is
// modelled in the foundation; remaining modules add their enums as they are built.

public enum StockMovementType
{
    PurchaseIn, SaleOut, PurchaseReturn, SaleReturn,
    TransferIn, TransferOut, Damaged, AdjustmentIn, AdjustmentOut, OpeningStock
}

public enum PaymentMethod { Cash, Card, UPI, BankTransfer, Wallet, StoreCredit, Cheque }

public enum PaymentStatus { Pending, Paid, PartiallyPaid, Failed, Refunded }

public enum PurchaseStatus { Draft, Confirmed, Cancelled }

public enum SaleStatus { Draft, Completed, Refunded, PartiallyRefunded, Cancelled }

public enum AuditAction { Create, Update, Delete, SoftDelete, Restore, Login, Logout }

public enum LoyaltyTxnType { Earn, Redeem, Expire, Adjust }

public enum EmploymentStatus { Active, OnLeave, Suspended, Resigned, Terminated }

public enum PayrollStatus { Draft, Generated, Approved, Paid }

public enum SalaryComponentKind { Earning, Deduction }

/// <summary>Category of a stored business document (drives filtering and expiry policy).</summary>
public enum DocumentType
{
    GstCertificate, RentAgreement, License, Insurance, Contract,
    SupplierDocument, EmployeeDocument, Invoice, BankStatement, Other
}

/// <summary>Daily attendance marking (mirrors the attendance_status enum in schema.sql).</summary>
public enum AttendanceStatus { Present, Absent, HalfDay, Leave, Holiday, WeekOff }

public enum LeaveType { Casual, Sick, Paid, Unpaid, Other }

public enum LeaveStatus { Pending, Approved, Rejected, Cancelled }

/// <summary>In-app notification categories (each maps to a live feed scanned on refresh).</summary>
public enum NotificationType { LowStock, DocumentExpiring, SalaryDue, LeavePending }

public enum NotificationSeverity { Info, Warning, Critical }

public enum WhatsAppMessageKind { Invoice, PaymentReminder, LowStockAlert, Promotion, Custom }

public enum WhatsAppMessageStatus { Pending, Sent, Failed }
