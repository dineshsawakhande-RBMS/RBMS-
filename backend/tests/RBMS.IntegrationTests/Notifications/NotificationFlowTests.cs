using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RBMS.Application.Common.Models;
using RBMS.Application.Features.Attendance.Commands;
using RBMS.Application.Features.Auth;
using RBMS.Application.Features.Employees.Commands;
using RBMS.Application.Features.Notifications;
using RBMS.Domain.Enums;
using Xunit;

namespace RBMS.IntegrationTests.Notifications;

public class NotificationFlowTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NotificationFlowTests(CustomWebApplicationFactory factory)
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
        var resp = await _client.PostAsJsonAsync("/api/employees", new CreateEmployeeCommand(
            EmployeeCode: code, FullName: "Neha Rao", Mobile: "9000000002", Email: null,
            Gender: null, DateOfBirth: null, Designation: null, Department: null,
            JoiningDate: new DateOnly(2026, 1, 1), MonthlyCtc: 20000,
            AddressLine1: null, City: null, State: null, Pincode: null,
            EmergencyContactName: null, EmergencyContactPhone: null,
            BankName: null, Ifsc: null, AccountLast4: null));
        return await resp.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<RefreshNotificationsResult> RefreshAsync()
        => (await (await _client.PostAsync("/api/notifications/refresh", null))
            .Content.ReadFromJsonAsync<RefreshNotificationsResult>())!;

    [Fact]
    public async Task Pending_leave_creates_a_notification_that_clears_on_approval()
    {
        await AuthenticateAsync();
        var empId = await CreateEmployeeAsync();

        var create = await _client.PostAsJsonAsync("/api/leaves", new CreateLeaveRequestCommand(
            empId, LeaveType.Casual, new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 11), "trip"));
        var leaveId = await create.Content.ReadFromJsonAsync<Guid>();

        var refresh1 = await RefreshAsync();
        refresh1.UnreadCount.Should().BeGreaterThanOrEqualTo(1);

        var list = await _client.GetFromJsonAsync<PagedResult<NotificationDto>>(
            "/api/notifications?unreadOnly=true", TestJson.Options);
        list!.Items.Should().Contain(n =>
            n.Type == NotificationType.LeavePending && n.RelatedEntityId == leaveId && n.LinkPath == "/attendance");

        // Approving the leave resolves the alert: a re-refresh clears it.
        await _client.PostAsJsonAsync($"/api/leaves/{leaveId}/decide", new { approve = true, decisionNotes = (string?)null });
        var refresh2 = await RefreshAsync();
        refresh2.Cleared.Should().BeGreaterThanOrEqualTo(1);

        var after = await _client.GetFromJsonAsync<PagedResult<NotificationDto>>(
            "/api/notifications", TestJson.Options);
        after!.Items.Should().NotContain(n => n.Type == NotificationType.LeavePending && n.RelatedEntityId == leaveId);
    }

    [Fact]
    public async Task Refresh_is_idempotent()
    {
        await AuthenticateAsync();
        var empId = await CreateEmployeeAsync();
        await _client.PostAsJsonAsync("/api/leaves", new CreateLeaveRequestCommand(
            empId, LeaveType.Sick, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1), null));

        var first = await RefreshAsync();
        first.Created.Should().BeGreaterThanOrEqualTo(1);
        var second = await RefreshAsync();
        second.Created.Should().Be(0);   // nothing new the second time
    }

    [Fact]
    public async Task Mark_read_decrements_unread_count()
    {
        await AuthenticateAsync();
        var empId = await CreateEmployeeAsync();
        await _client.PostAsJsonAsync("/api/leaves", new CreateLeaveRequestCommand(
            empId, LeaveType.Casual, new DateOnly(2026, 6, 20), new DateOnly(2026, 6, 20), null));
        await RefreshAsync();

        var before = await _client.GetFromJsonAsync<int>("/api/notifications/count");
        before.Should().BeGreaterThanOrEqualTo(1);

        var unread = await _client.GetFromJsonAsync<PagedResult<NotificationDto>>(
            "/api/notifications?unreadOnly=true", TestJson.Options);
        var id = unread!.Items.First().Id;

        var read = await _client.PostAsync($"/api/notifications/{id}/read", null);
        read.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await _client.GetFromJsonAsync<int>("/api/notifications/count");
        after.Should().Be(before - 1);
    }
}
