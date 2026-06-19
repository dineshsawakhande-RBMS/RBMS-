namespace RBMS.Application.Features.Suppliers;

public record SupplierListItemDto(
    Guid Id, string Code, string Name, string? Phone, string? Gstin,
    decimal OutstandingBalance, bool IsActive);

public record SupplierDto(
    Guid Id, string Code, string Name, string? Gstin, string? ContactPerson,
    string? Phone, string? Email, string? AddressLine1, string? City, string? State,
    string? Pincode, int PaymentTermsDays, decimal OutstandingBalance, bool IsActive);

public record SupplierLedgerEntryDto(
    DateOnly EntryDate, string ReferenceType, decimal Debit, decimal Credit,
    decimal RunningBalance, string? Notes);

public record SupplierLedgerDto(
    Guid SupplierId, string Name, decimal Outstanding, IReadOnlyList<SupplierLedgerEntryDto> Entries);
