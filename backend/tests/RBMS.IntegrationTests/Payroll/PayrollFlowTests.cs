using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RBMS.Application.Features.Auth;
using RBMS.Application.Features.Employees.Commands;
using RBMS.Application.Features.Payroll;
using RBMS.Application.Features.Payroll.Commands;
using Xunit;

namespace RBMS.IntegrationTests.Payroll;

public class PayrollFlowTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PayrollFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.SeedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task AuthenticateAsync()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginCommand(CustomWebApplicationFactory.Seed.Username, CustomWebApplicationFactory.Seed.Password));
        var auth = await login.Content.ReadFromJsonAsync<AuthResultDto>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    private async Task<Guid> CreateEmployeeAsync(decimal ctc)
    {
        var resp = await _client.PostAsJsonAsync("/api/employees", new CreateEmployeeCommand(
            EmployeeCode: $"E{Guid.NewGuid():N}".Substring(0, 10), FullName: "Pay Test", Mobile: "9000000001",
            Email: null, Gender: null, DateOfBirth: null, Designation: "Cashier", Department: "Store",
            JoiningDate: new DateOnly(2026, 1, 1), MonthlyCtc: ctc,
            AddressLine1: null, City: null, State: null, Pincode: null,
            EmergencyContactName: null, EmergencyContactPhone: null, BankName: null, Ifsc: null, AccountLast4: null));
        return await resp.Content.ReadFromJsonAsync<Guid>();
    }

    [Fact]
    public async Task Full_month_payroll_recovers_advance_and_computes_net()
    {
        await AuthenticateAsync();
        var empId = await CreateEmployeeAsync(30000);

        // ₹5,000 advance, then full-attendance payroll: gross 30000 − 5000 recovery = 25000 net.
        await _client.PostAsJsonAsync("/api/payroll/advances",
            new CreateSalaryAdvanceCommand(empId, 5000, new DateOnly(2026, 6, 5), "Festival advance"));

        var gen = await _client.PostAsJsonAsync("/api/payroll/generate",
            new GeneratePayrollCommand(empId, 2026, 6, WorkingDays: 30, PresentDays: 30, Bonus: 0, Deductions: 0));
        gen.StatusCode.Should().Be(HttpStatusCode.OK);
        var payrollId = await gen.Content.ReadFromJsonAsync<Guid>();

        var detail = await _client.GetFromJsonAsync<PayrollDto>($"/api/payroll/{payrollId}", TestJson.Options);
        detail!.GrossEarnings.Should().Be(30000);
        detail.AdvanceDeducted.Should().Be(5000);
        detail.NetPay.Should().Be(25000);

        var pay = await _client.PostAsync($"/api/payroll/{payrollId}/pay", null);
        pay.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Half_attendance_prorates_gross()
    {
        await AuthenticateAsync();
        var empId = await CreateEmployeeAsync(30000);

        var gen = await _client.PostAsJsonAsync("/api/payroll/generate",
            new GeneratePayrollCommand(empId, 2026, 5, WorkingDays: 30, PresentDays: 15, Bonus: 1000, Deductions: 0));
        var payrollId = await gen.Content.ReadFromJsonAsync<Guid>();

        var detail = await _client.GetFromJsonAsync<PayrollDto>($"/api/payroll/{payrollId}", TestJson.Options);
        detail!.GrossEarnings.Should().Be(15000); // 30000 * 15/30
        detail.NetPay.Should().Be(16000);          // 15000 + 1000 bonus
    }

    [Fact]
    public async Task Duplicate_period_is_rejected()
    {
        await AuthenticateAsync();
        var empId = await CreateEmployeeAsync(20000);
        await _client.PostAsJsonAsync("/api/payroll/generate",
            new GeneratePayrollCommand(empId, 2026, 4, 30, 30, 0, 0));
        var dup = await _client.PostAsJsonAsync("/api/payroll/generate",
            new GeneratePayrollCommand(empId, 2026, 4, 30, 30, 0, 0));
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
