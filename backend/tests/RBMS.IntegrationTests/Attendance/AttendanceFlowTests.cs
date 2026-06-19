using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RBMS.Application.Common.Models;
using RBMS.Application.Features.Attendance;
using RBMS.Application.Features.Attendance.Commands;
using RBMS.Application.Features.Auth;
using RBMS.Application.Features.Employees.Commands;
using RBMS.Domain.Enums;
using Xunit;

namespace RBMS.IntegrationTests.Attendance;

public class AttendanceFlowTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AttendanceFlowTests(CustomWebApplicationFactory factory)
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

    private async Task<Guid> CreateEmployeeAsync()
    {
        var code = $"EMP-{Guid.NewGuid():N}".Substring(0, 12);
        var cmd = new CreateEmployeeCommand(
            EmployeeCode: code, FullName: "Asha Verma", Mobile: "9000000001", Email: null,
            Gender: null, DateOfBirth: null, Designation: "Sales", Department: "Store",
            JoiningDate: new DateOnly(2026, 1, 1), MonthlyCtc: 30000,
            AddressLine1: null, City: null, State: null, Pincode: null,
            EmergencyContactName: null, EmergencyContactPhone: null,
            BankName: null, Ifsc: null, AccountLast4: null);
        var resp = await _client.PostAsJsonAsync("/api/employees", cmd);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return await resp.Content.ReadFromJsonAsync<Guid>();
    }

    private static AttendanceEntryInput Day(int day, AttendanceStatus status) =>
        new(new DateOnly(2026, 6, day), status, null, null, null);

    [Fact]
    public async Task Marking_a_month_produces_correct_summary()
    {
        await AuthenticateAsync();
        var empId = await CreateEmployeeAsync();

        var entries = new List<AttendanceEntryInput>();
        for (var d = 1; d <= 20; d++) entries.Add(Day(d, AttendanceStatus.Present));
        entries.Add(Day(21, AttendanceStatus.Absent));
        entries.Add(Day(22, AttendanceStatus.Absent));
        entries.Add(Day(23, AttendanceStatus.HalfDay));
        entries.Add(Day(24, AttendanceStatus.HalfDay));
        entries.Add(Day(25, AttendanceStatus.WeekOff));

        var mark = await _client.PostAsJsonAsync("/api/attendance", new MarkAttendanceCommand(empId, entries));
        mark.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await _client.GetFromJsonAsync<AttendanceSummaryDto>(
            $"/api/attendance/summary?employeeId={empId}&year=2026&month=6", TestJson.Options);

        summary!.WorkingDays.Should().Be(24);   // 20 present + 2 absent + 2 half (week-off excluded)
        summary.PresentDays.Should().Be(21);     // 20 + 0.5*2
        summary.Present.Should().Be(20);
        summary.WeekOff.Should().Be(1);

        var monthly = await _client.GetFromJsonAsync<List<AttendanceDto>>(
            $"/api/attendance?employeeId={empId}&year=2026&month=6", TestJson.Options);
        monthly!.Should().HaveCount(25);
    }

    [Fact]
    public async Task Re_marking_a_day_updates_in_place()
    {
        await AuthenticateAsync();
        var empId = await CreateEmployeeAsync();

        await _client.PostAsJsonAsync("/api/attendance",
            new MarkAttendanceCommand(empId, new[] { Day(10, AttendanceStatus.Present) }));
        await _client.PostAsJsonAsync("/api/attendance",
            new MarkAttendanceCommand(empId, new[] { Day(10, AttendanceStatus.Absent) }));

        var monthly = await _client.GetFromJsonAsync<List<AttendanceDto>>(
            $"/api/attendance?employeeId={empId}&year=2026&month=6", TestJson.Options);
        monthly!.Should().ContainSingle();
        monthly[0].Status.Should().Be(AttendanceStatus.Absent);
    }

    [Fact]
    public async Task Approving_leave_marks_attendance_and_feeds_summary()
    {
        await AuthenticateAsync();
        var empId = await CreateEmployeeAsync();

        var create = await _client.PostAsJsonAsync("/api/leaves", new CreateLeaveRequestCommand(
            empId, LeaveType.Casual, new DateOnly(2026, 6, 26), new DateOnly(2026, 6, 27), "family event"));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var leaveId = await create.Content.ReadFromJsonAsync<Guid>();

        var decide = await _client.PostAsJsonAsync($"/api/leaves/{leaveId}/decide",
            new { approve = true, decisionNotes = "ok" });
        decide.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Two Leave days now exist in attendance (working days, zero present credit).
        var summary = await _client.GetFromJsonAsync<AttendanceSummaryDto>(
            $"/api/attendance/summary?employeeId={empId}&year=2026&month=6", TestJson.Options);
        summary!.Leave.Should().Be(2);
        summary.WorkingDays.Should().Be(2);
        summary.PresentDays.Should().Be(0);

        var leaves = await _client.GetFromJsonAsync<PagedResult<LeaveRequestDto>>(
            $"/api/leaves?employeeId={empId}", TestJson.Options);
        leaves!.Items.Should().ContainSingle(l => l.Id == leaveId && l.Status == LeaveStatus.Approved);
    }

    [Fact]
    public async Task Deciding_an_already_decided_leave_conflicts()
    {
        await AuthenticateAsync();
        var empId = await CreateEmployeeAsync();
        var create = await _client.PostAsJsonAsync("/api/leaves", new CreateLeaveRequestCommand(
            empId, LeaveType.Sick, new DateOnly(2026, 6, 5), new DateOnly(2026, 6, 5), null));
        var leaveId = await create.Content.ReadFromJsonAsync<Guid>();

        await _client.PostAsJsonAsync($"/api/leaves/{leaveId}/decide", new { approve = true, decisionNotes = (string?)null });
        var second = await _client.PostAsJsonAsync($"/api/leaves/{leaveId}/decide", new { approve = false, decisionNotes = (string?)null });
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
