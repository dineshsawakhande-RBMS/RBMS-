"use client";

import { useEffect, useMemo, useState } from "react";
import { AxiosError } from "axios";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Card from "@mui/material/Card";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import MenuItem from "@mui/material/MenuItem";
import Tabs from "@mui/material/Tabs";
import Tab from "@mui/material/Tab";
import Chip from "@mui/material/Chip";
import LinearProgress from "@mui/material/LinearProgress";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import TablePagination from "@mui/material/TablePagination";
import Dialog from "@mui/material/Dialog";
import DialogTitle from "@mui/material/DialogTitle";
import DialogContent from "@mui/material/DialogContent";
import DialogActions from "@mui/material/DialogActions";
import Alert from "@mui/material/Alert";
import SaveIcon from "@mui/icons-material/Save";
import AddIcon from "@mui/icons-material/Add";
import CheckIcon from "@mui/icons-material/Check";
import CloseIcon from "@mui/icons-material/Close";
import type { AttendanceStatus, LeaveType } from "@/types";
import { useEmployees } from "@/features/employees/hooks";
import {
  useMonthlyAttendance, useAttendanceSummary, useMarkAttendance,
  useLeaves, useCreateLeave, useDecideLeave,
} from "@/features/attendance/hooks";
import { ATTENDANCE_STATUSES, LEAVE_TYPES, attendanceColor, leaveStatusColor, MONTHS } from "@/features/attendance/constants";
import { useToast } from "@/components/providers/ToastProvider";

const now = new Date();
const today = () => new Date().toISOString().slice(0, 10);
const pad = (n: number) => String(n).padStart(2, "0");
const dateStr = (y: number, m: number, d: number) => `${y}-${pad(m)}-${pad(d)}`;

export default function AttendancePage() {
  const toast = useToast();
  const [tab, setTab] = useState(0);
  const [employeeId, setEmployeeId] = useState("");
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);

  const { data: employees } = useEmployees({ page: 1, pageSize: 100 });
  const { data: records, isFetching } = useMonthlyAttendance(employeeId || null, year, month);
  const { data: summary } = useAttendanceSummary(employeeId || null, year, month);
  const mark = useMarkAttendance();

  // Local per-day status edits, keyed by day-of-month. "" = unset.
  const [marks, setMarks] = useState<Record<number, AttendanceStatus | "">>({});

  const daysInMonth = useMemo(() => new Date(year, month, 0).getDate(), [year, month]);

  useEffect(() => {
    const next: Record<number, AttendanceStatus | ""> = {};
    for (const r of records ?? []) next[Number(r.workDate.slice(8, 10))] = r.status;
    setMarks(next);
  }, [records]);

  const years = [now.getFullYear(), now.getFullYear() - 1, now.getFullYear() - 2];
  const setDay = (day: number, status: AttendanceStatus | "") => setMarks((m) => ({ ...m, [day]: status }));
  const fillUnset = () =>
    setMarks((m) => {
      const next = { ...m };
      for (let d = 1; d <= daysInMonth; d++) if (!next[d]) next[d] = "Present";
      return next;
    });

  const saveAttendance = async () => {
    const entries = Object.entries(marks)
      .filter(([, s]) => s !== "")
      .map(([d, s]) => ({ workDate: dateStr(year, month, Number(d)), status: s as AttendanceStatus }));
    if (entries.length === 0) { toast("Nothing to save", "info"); return; }
    try {
      await mark.mutateAsync({ employeeId, entries });
      toast(`Saved ${entries.length} day(s)`);
    } catch {
      toast("Could not save attendance", "error");
    }
  };

  return (
    <Box>
      <Typography variant="h1" sx={{ mb: 2 }}>Attendance & Leave</Typography>

      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 2 }}>
        <Tab label="Attendance" />
        <Tab label="Leave" />
      </Tabs>

      {tab === 0 && (
        <>
          <Card elevation={0} sx={{ p: 2, mb: 2 }}>
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2} alignItems={{ tablet: "center" }}>
              <TextField select label="Employee" value={employeeId} onChange={(e) => setEmployeeId(e.target.value)} sx={{ minWidth: 260 }}>
                {(employees?.items ?? []).map((emp) => <MenuItem key={emp.id} value={emp.id}>{emp.employeeCode} — {emp.fullName}</MenuItem>)}
              </TextField>
              <TextField select label="Month" value={month} onChange={(e) => setMonth(Number(e.target.value))} sx={{ width: 130 }}>
                {MONTHS.map((m, i) => <MenuItem key={m} value={i + 1}>{m}</MenuItem>)}
              </TextField>
              <TextField select label="Year" value={year} onChange={(e) => setYear(Number(e.target.value))} sx={{ width: 110 }}>
                {years.map((y) => <MenuItem key={y} value={y}>{y}</MenuItem>)}
              </TextField>
            </Stack>
            {summary && (
              <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap sx={{ mt: 2 }}>
                <Chip color="primary" label={`Working days: ${summary.workingDays}`} />
                <Chip color="success" label={`Present days: ${summary.presentDays}`} />
                <Chip variant="outlined" label={`P ${summary.present}`} />
                <Chip variant="outlined" label={`A ${summary.absent}`} />
                <Chip variant="outlined" label={`½ ${summary.halfDay}`} />
                <Chip variant="outlined" label={`Leave ${summary.leave}`} />
                <Chip variant="outlined" label={`Holiday ${summary.holiday}`} />
                <Chip variant="outlined" label={`WeekOff ${summary.weekOff}`} />
              </Stack>
            )}
          </Card>

          {!employeeId ? (
            <Alert severity="info">Select an employee to mark attendance.</Alert>
          ) : (
            <Card elevation={0}>
              <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ p: 2 }}>
                <Button size="small" onClick={fillUnset}>Fill unset as Present</Button>
                <Button variant="contained" startIcon={<SaveIcon />} disabled={mark.isPending} onClick={saveAttendance}>
                  {mark.isPending ? "Saving…" : "Save"}
                </Button>
              </Stack>
              {isFetching && <LinearProgress />}
              <TableContainer sx={{ maxHeight: 560 }}>
                <Table stickyHeader size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Date</TableCell>
                      <TableCell>Day</TableCell>
                      <TableCell>Status</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {Array.from({ length: daysInMonth }, (_, i) => i + 1).map((d) => {
                      const weekday = new Date(year, month - 1, d).toLocaleDateString(undefined, { weekday: "short" });
                      const status = marks[d] ?? "";
                      return (
                        <TableRow key={d} hover>
                          <TableCell>{pad(d)} {MONTHS[month - 1]}</TableCell>
                          <TableCell sx={{ color: "text.secondary" }}>{weekday}</TableCell>
                          <TableCell>
                            <TextField
                              select size="small" value={status} onChange={(e) => setDay(d, e.target.value as AttendanceStatus | "")}
                              sx={{ minWidth: 150 }}
                              SelectProps={{ displayEmpty: true }}
                            >
                              <MenuItem value=""><em>—</em></MenuItem>
                              {ATTENDANCE_STATUSES.map((s) => <MenuItem key={s.value} value={s.value}>{s.label}</MenuItem>)}
                            </TextField>
                            {status && (
                              <Chip size="small" sx={{ ml: 1 }} color={attendanceColor(status)} label={status} />
                            )}
                          </TableCell>
                        </TableRow>
                      );
                    })}
                  </TableBody>
                </Table>
              </TableContainer>
            </Card>
          )}
        </>
      )}

      {tab === 1 && <LeaveTab />}
    </Box>
  );
}

function LeaveTab() {
  const toast = useToast();
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState({ employeeId: "", leaveType: "Casual" as LeaveType, fromDate: today(), toDate: today(), reason: "" });

  const { data: employees } = useEmployees({ page: 1, pageSize: 100 });
  const { data, isFetching } = useLeaves({ page: page + 1, pageSize });
  const create = useCreateLeave();
  const decide = useDecideLeave();

  const submit = async () => {
    setError(null);
    try {
      await create.mutateAsync({
        employeeId: form.employeeId, leaveType: form.leaveType,
        fromDate: form.fromDate, toDate: form.toDate, reason: form.reason || null,
      });
      toast("Leave request submitted");
      setOpen(false);
      setForm({ employeeId: "", leaveType: "Casual", fromDate: today(), toDate: today(), reason: "" });
    } catch (err) {
      setError((err as AxiosError<{ title?: string }>).response?.data?.title ?? "Could not submit leave.");
    }
  };

  const act = async (id: string, approve: boolean) => {
    try {
      await decide.mutateAsync({ id, approve });
      toast(approve ? "Leave approved" : "Leave rejected", approve ? "success" : "info");
    } catch {
      toast("Could not update leave", "error");
    }
  };

  return (
    <Card elevation={0}>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ p: 2 }}>
        <Typography variant="h6">Leave requests</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setOpen(true)}>Request leave</Button>
      </Stack>
      {isFetching && <LinearProgress />}
      <TableContainer>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Employee</TableCell>
              <TableCell>Type</TableCell>
              <TableCell>From</TableCell>
              <TableCell>To</TableCell>
              <TableCell align="right">Days</TableCell>
              <TableCell align="center">Status</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {data?.items.map((l) => (
              <TableRow key={l.id} hover>
                <TableCell>{l.employeeName}</TableCell>
                <TableCell>{l.leaveType}</TableCell>
                <TableCell>{l.fromDate}</TableCell>
                <TableCell>{l.toDate}</TableCell>
                <TableCell align="right">{l.days}</TableCell>
                <TableCell align="center"><Chip size="small" color={leaveStatusColor(l.status)} label={l.status} /></TableCell>
                <TableCell align="right">
                  {l.status === "Pending" && (
                    <>
                      <Button size="small" color="success" startIcon={<CheckIcon />} disabled={decide.isPending} onClick={() => act(l.id, true)}>Approve</Button>
                      <Button size="small" color="error" startIcon={<CloseIcon />} disabled={decide.isPending} onClick={() => act(l.id, false)}>Reject</Button>
                    </>
                  )}
                </TableCell>
              </TableRow>
            ))}
            {data && data.items.length === 0 && (
              <TableRow><TableCell colSpan={7} align="center" sx={{ py: 4, color: "text.secondary" }}>No leave requests.</TableCell></TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>
      <TablePagination
        component="div" count={data?.totalCount ?? 0} page={page}
        onPageChange={(_, p) => setPage(p)} rowsPerPage={pageSize}
        onRowsPerPageChange={(e) => { setPageSize(parseInt(e.target.value, 10)); setPage(0); }}
        rowsPerPageOptions={[10, 20, 50]}
      />

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>Request Leave</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField select required label="Employee" value={form.employeeId} onChange={(e) => setForm({ ...form, employeeId: e.target.value })}>
              {employees?.items.map((emp) => <MenuItem key={emp.id} value={emp.id}>{emp.employeeCode} — {emp.fullName}</MenuItem>)}
            </TextField>
            <TextField select label="Leave type" value={form.leaveType} onChange={(e) => setForm({ ...form, leaveType: e.target.value as LeaveType })}>
              {LEAVE_TYPES.map((t) => <MenuItem key={t} value={t}>{t}</MenuItem>)}
            </TextField>
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="From" type="date" value={form.fromDate} onChange={(e) => setForm({ ...form, fromDate: e.target.value })} InputLabelProps={{ shrink: true }} sx={{ flex: 1 }} />
              <TextField label="To" type="date" value={form.toDate} onChange={(e) => setForm({ ...form, toDate: e.target.value })} InputLabelProps={{ shrink: true }} sx={{ flex: 1 }} />
            </Stack>
            <TextField label="Reason" value={form.reason} onChange={(e) => setForm({ ...form, reason: e.target.value })} multiline minRows={2} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button variant="contained" disabled={!form.employeeId || create.isPending} onClick={submit}>
            {create.isPending ? "Submitting…" : "Submit"}
          </Button>
        </DialogActions>
      </Dialog>
    </Card>
  );
}
