using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Payroll;

public record PayrollListItemDto(
    Guid Id, string EmployeeName, int PeriodYear, int PeriodMonth,
    decimal GrossEarnings, decimal TotalDeductions, decimal NetPay, PayrollStatus Status);

public record PayrollLineDto(string Name, SalaryComponentKind Kind, decimal Amount);

public record PayrollDto(
    Guid Id, string EmployeeName, string EmployeeCode, int PeriodYear, int PeriodMonth,
    decimal WorkingDays, decimal PresentDays, decimal GrossEarnings, decimal Bonus,
    decimal TotalDeductions, decimal AdvanceDeducted, decimal NetPay, PayrollStatus Status,
    IReadOnlyList<PayrollLineDto> Lines);

public record SalaryAdvanceDto(
    Guid Id, string EmployeeName, decimal Amount, DateOnly AdvanceDate,
    decimal Recovered, decimal Outstanding, string? Notes);
