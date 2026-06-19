using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RBMS.Application.Features.Auth;
using RBMS.Application.Features.Employees;
using RBMS.Application.Features.Employees.Commands;
using Xunit;

namespace RBMS.IntegrationTests.Employees;

public class EmployeeFlowTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EmployeeFlowTests(CustomWebApplicationFactory factory)
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

    private static CreateEmployeeCommand New(string code) => new(
        EmployeeCode: code, FullName: "Priya Nair", Mobile: "9876543210", Email: "priya@shop.test",
        Gender: "Female", DateOfBirth: new DateOnly(1996, 4, 12), Designation: "Sales Associate",
        Department: "Store", JoiningDate: new DateOnly(2026, 1, 10), MonthlyCtc: 25000,
        AddressLine1: null, City: "Pune", State: "MH", Pincode: null,
        EmergencyContactName: null, EmergencyContactPhone: null,
        BankName: "HDFC", Ifsc: "HDFC0001234", AccountLast4: "6789");

    [Fact]
    public async Task Create_then_fetch_employee()
    {
        await AuthenticateAsync();
        var resp = await _client.PostAsJsonAsync("/api/employees", New($"EMP-{Guid.NewGuid():N}".Substring(0, 12)));
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await resp.Content.ReadFromJsonAsync<Guid>();

        var detail = await _client.GetFromJsonAsync<EmployeeDto>($"/api/employees/{id}", TestJson.Options);
        detail!.FullName.Should().Be("Priya Nair");
        detail.MonthlyCtc.Should().Be(25000);
        detail.Designation.Should().Be("Sales Associate");
    }

    [Fact]
    public async Task Duplicate_code_is_rejected()
    {
        await AuthenticateAsync();
        var code = $"EMP-{Guid.NewGuid():N}".Substring(0, 12);
        await _client.PostAsJsonAsync("/api/employees", New(code));
        var second = await _client.PostAsJsonAsync("/api/employees", New(code));
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
