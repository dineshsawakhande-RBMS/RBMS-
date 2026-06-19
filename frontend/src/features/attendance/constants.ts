import type { AttendanceStatus, LeaveStatus, LeaveType } from "@/types";

export const ATTENDANCE_STATUSES: { value: AttendanceStatus; label: string; short: string }[] = [
  { value: "Present", label: "Present", short: "P" },
  { value: "Absent", label: "Absent", short: "A" },
  { value: "HalfDay", label: "Half Day", short: "½" },
  { value: "Leave", label: "Leave", short: "L" },
  { value: "Holiday", label: "Holiday", short: "H" },
  { value: "WeekOff", label: "Week Off", short: "W" },
];

export const LEAVE_TYPES: LeaveType[] = ["Casual", "Sick", "Paid", "Unpaid", "Other"];

type Color = "success" | "error" | "warning" | "info" | "default" | "primary";

export const attendanceColor = (s: AttendanceStatus): Color =>
  s === "Present" ? "success"
    : s === "Absent" ? "error"
    : s === "HalfDay" ? "warning"
    : s === "Leave" ? "info"
    : "default";

export const leaveStatusColor = (s: LeaveStatus): Color =>
  s === "Approved" ? "success" : s === "Rejected" ? "error" : s === "Cancelled" ? "default" : "warning";

export const MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
