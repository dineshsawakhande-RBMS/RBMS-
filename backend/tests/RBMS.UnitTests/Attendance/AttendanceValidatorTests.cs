using FluentValidation.TestHelper;
using RBMS.Application.Features.Attendance;
using RBMS.Application.Features.Attendance.Commands;
using RBMS.Domain.Enums;
using Xunit;

namespace RBMS.UnitTests.Attendance;

public class AttendanceValidatorTests
{
    private readonly MarkAttendanceCommandValidator _mark = new();
    private readonly CreateLeaveRequestCommandValidator _leave = new();

    private static MarkAttendanceCommand MarkOf(params AttendanceEntryInput[] entries) =>
        new(Guid.NewGuid(), entries);

    private static AttendanceEntryInput Entry(int day, AttendanceStatus status = AttendanceStatus.Present) =>
        new(new DateOnly(2026, 6, day), status, null, null, null);

    [Fact]
    public void Valid_mark_passes()
        => _mark.TestValidate(MarkOf(Entry(1), Entry(2))).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Empty_entries_fail()
        => _mark.TestValidate(MarkOf()).ShouldHaveValidationErrorFor(c => c.Entries);

    [Fact]
    public void Duplicate_dates_fail()
        => _mark.TestValidate(MarkOf(Entry(5), Entry(5, AttendanceStatus.Absent)))
            .ShouldHaveValidationErrorFor(c => c.Entries);

    [Fact]
    public void Valid_leave_passes()
        => _leave.TestValidate(new CreateLeaveRequestCommand(
                Guid.NewGuid(), LeaveType.Casual, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 3), "trip"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Leave_end_before_start_fails()
        => _leave.TestValidate(new CreateLeaveRequestCommand(
                Guid.NewGuid(), LeaveType.Casual, new DateOnly(2026, 6, 3), new DateOnly(2026, 6, 1), null))
            .ShouldHaveValidationErrorFor(c => c.ToDate);

    [Theory]
    [InlineData(AttendanceStatus.Present, 1.0)]
    [InlineData(AttendanceStatus.HalfDay, 0.5)]
    [InlineData(AttendanceStatus.Absent, 0.0)]
    [InlineData(AttendanceStatus.Leave, 0.0)]
    [InlineData(AttendanceStatus.Holiday, 0.0)]
    public void Present_credit_is_correct(AttendanceStatus status, double expected)
        => Assert.Equal((decimal)expected, AttendanceMath.PresentCredit(status));

    [Theory]
    [InlineData(AttendanceStatus.Present, true)]
    [InlineData(AttendanceStatus.Holiday, false)]
    [InlineData(AttendanceStatus.WeekOff, false)]
    public void Working_day_classification_is_correct(AttendanceStatus status, bool expected)
        => Assert.Equal(expected, AttendanceMath.CountsAsWorkingDay(status));
}
