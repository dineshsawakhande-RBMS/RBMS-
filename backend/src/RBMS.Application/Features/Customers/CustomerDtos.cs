namespace RBMS.Application.Features.Customers;

public record CustomerListItemDto(
    Guid Id, string Name, string Mobile, string? Email, int LoyaltyPoints, bool IsActive);

public record CustomerDto(
    Guid Id, string Name, string Mobile, string? Email, string? AddressLine1, string? City,
    string? State, string? Pincode, DateOnly? Birthday, DateOnly? Anniversary,
    int LoyaltyPoints, bool IsActive);
