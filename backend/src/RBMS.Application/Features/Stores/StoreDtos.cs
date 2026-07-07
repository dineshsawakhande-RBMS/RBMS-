namespace RBMS.Application.Features.Stores;

public record StoreListItemDto(
    Guid Id, string Code, string Name, string? City, string? Phone, bool IsActive);

public record StoreDto(
    Guid Id, string Code, string Name, string? Gstin, string? Phone, string? Email,
    string? AddressLine1, string? City, string? State, string? Pincode, bool IsActive);
