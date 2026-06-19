using RBMS.Domain.Common;
using RBMS.Domain.Enums;

namespace RBMS.Domain.Entities;

public class Employee : AuditableEntity
{
    public Guid? StoreId { get; set; }
    public string EmployeeCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string Mobile { get; set; } = null!;
    public string? Email { get; set; }
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public DateOnly JoiningDate { get; set; }
    public DateOnly? ExitDate { get; set; }
    public EmploymentStatus Status { get; set; } = EmploymentStatus.Active;

    /// <summary>Monthly cost-to-company — the basis for payroll (Salary module).</summary>
    public decimal MonthlyCtc { get; set; }

    // Bank (full account number deferred to the secure Documents pass; last 4 only for now).
    public string? BankName { get; set; }
    public string? Ifsc { get; set; }
    public string? AccountLast4 { get; set; }
}
