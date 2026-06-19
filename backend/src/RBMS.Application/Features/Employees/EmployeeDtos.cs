using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Employees;

public record EmployeeListItemDto(
    Guid Id, string EmployeeCode, string FullName, string? Designation,
    string Mobile, EmploymentStatus Status, decimal MonthlyCtc);

public record EmployeeDto(
    Guid Id, string EmployeeCode, string FullName, string? Gender, DateOnly? DateOfBirth,
    string Mobile, string? Email, string? AddressLine1, string? City, string? State, string? Pincode,
    string? EmergencyContactName, string? EmergencyContactPhone, string? Designation, string? Department,
    DateOnly JoiningDate, DateOnly? ExitDate, EmploymentStatus Status, decimal MonthlyCtc,
    string? BankName, string? Ifsc, string? AccountLast4);
