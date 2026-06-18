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
