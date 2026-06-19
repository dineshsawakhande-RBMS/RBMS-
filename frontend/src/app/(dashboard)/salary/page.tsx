"use client";

import { useState } from "react";
import { AxiosError } from "axios";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Card from "@mui/material/Card";
import Button from "@mui/material/Button";
import IconButton from "@mui/material/IconButton";
import Tooltip from "@mui/material/Tooltip";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import MenuItem from "@mui/material/MenuItem";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Chip from "@mui/material/Chip";
import LinearProgress from "@mui/material/LinearProgress";
import Dialog from "@mui/material/Dialog";
import DialogTitle from "@mui/material/DialogTitle";
import DialogContent from "@mui/material/DialogContent";
import DialogActions from "@mui/material/DialogActions";
import Alert from "@mui/material/Alert";
import AddIcon from "@mui/icons-material/Add";
import PaymentsIcon from "@mui/icons-material/Payments";
import ReceiptIcon from "@mui/icons-material/Receipt";
import DoneIcon from "@mui/icons-material/Done";
import { usePayrolls, useGeneratePayroll, useMarkPayrollPaid, useCreateAdvance, downloadSlip } from "@/features/payroll/hooks";
import { useEmployees } from "@/features/employees/hooks";
import { useToast } from "@/components/providers/ToastProvider";
import { formatMoney } from "@/lib/config";

const MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
const now = new Date();
const today = () => new Date().toISOString().slice(0, 10);
const statusColor = (s: string) => (s === "Paid" ? "success" : s === "Approved" ? "primary" : "info");

export default function SalaryPage() {
  const toast = useToast();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const [genOpen, setGenOpen] = useState(false);
  const [advOpen, setAdvOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [gen, setGen] = useState({ employeeId: "", workingDays: 30, presentDays: 30, bonus: 0, deductions: 0 });
  const [adv, setAdv] = useState({ employeeId: "", amount: 0, advanceDate: today(), notes: "" });

  const { data: payrolls, isFetching } = usePayrolls(year, month);
  const { data: employees } = useEmployees({ page: 1, pageSize: 100 });
  const generate = useGeneratePayroll();
  const markPaid = useMarkPayrollPaid();
  const createAdvance = useCreateAdvance();

  const handleGenerate = async () => {
    setError(null);
    try {
      await generate.mutateAsync({ ...gen, periodYear: year, periodMonth: month, workingDays: Number(gen.workingDays), presentDays: Number(gen.presentDays), bonus: Number(gen.bonus), deductions: Number(gen.deductions) });
      toast("Payroll generated");
      setGenOpen(false);
      setGen({ employeeId: "", workingDays: 30, presentDays: 30, bonus: 0, deductions: 0 });
    } catch (err) {
      const e = err as AxiosError<{ title?: string }>;
      setError(e.response?.status === 409 ? "Payroll for this employee/month already exists." : (e.response?.data?.title ?? "Could not generate payroll."));
    }
  };

  const handleAdvance = async () => {
    setError(null);
    try {
      await createAdvance.mutateAsync({ employeeId: adv.employeeId, amount: Number(adv.amount), advanceDate: adv.advanceDate, notes: adv.notes || null });
      toast("Advance recorded");
      setAdvOpen(false);
      setAdv({ employeeId: "", amount: 0, advanceDate: today(), notes: "" });
    } catch (err) {
      setError((err as AxiosError<{ title?: string }>).response?.data?.title ?? "Could not record advance.");
    }
  };

  const pay = async (id: string) => {
    try { await markPaid.mutateAsync(id); toast("Marked as paid"); }
    catch { toast("Could not mark paid", "error"); }
  };

  const years = [now.getFullYear(), now.getFullYear() - 1, now.getFullYear() - 2];

  return (
    <Box>
      <Stack direction={{ xs: "column", tablet: "row" }} justifyContent="space-between" alignItems={{ tablet: "center" }} spacing={2} sx={{ mb: 2 }}>
        <Typography variant="h1">Salary</Typography>
        <Stack direction="row" spacing={1}>
          <Button variant="outlined" startIcon={<PaymentsIcon />} onClick={() => setAdvOpen(true)}>Advance</Button>
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setGenOpen(true)}>Generate</Button>
        </Stack>
      </Stack>

      <Card elevation={0} sx={{ p: 2, mb: 2 }}>
        <Stack direction="row" spacing={2}>
          <TextField select label="Month" value={month} onChange={(e) => setMonth(Number(e.target.value))} sx={{ width: 140 }}>
            {MONTHS.map((m, i) => <MenuItem key={m} value={i + 1}>{m}</MenuItem>)}
          </TextField>
          <TextField select label="Year" value={year} onChange={(e) => setYear(Number(e.target.value))} sx={{ width: 120 }}>
            {years.map((y) => <MenuItem key={y} value={y}>{y}</MenuItem>)}
          </TextField>
        </Stack>
      </Card>

      <Card elevation={0}>
        {isFetching && <LinearProgress />}
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Employee</TableCell>
                <TableCell align="right">Gross</TableCell>
                <TableCell align="right">Deductions</TableCell>
                <TableCell align="right">Net Pay</TableCell>
                <TableCell align="center">Status</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {payrolls?.map((p) => (
                <TableRow key={p.id} hover>
                  <TableCell>{p.employeeName}</TableCell>
                  <TableCell align="right">{formatMoney(p.grossEarnings)}</TableCell>
                  <TableCell align="right">{formatMoney(p.totalDeductions)}</TableCell>
                  <TableCell align="right"><strong>{formatMoney(p.netPay)}</strong></TableCell>
                  <TableCell align="center"><Chip size="small" color={statusColor(p.status)} label={p.status} /></TableCell>
                  <TableCell align="right">
                    <Tooltip title="Salary slip (PDF)">
                      <IconButton size="small" onClick={() => downloadSlip(p.id, `${p.employeeName}-${MONTHS[p.periodMonth - 1]}${p.periodYear}`)}>
                        <ReceiptIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    {p.status !== "Paid" && (
                      <Tooltip title="Mark paid">
                        <IconButton size="small" color="success" onClick={() => pay(p.id)}>
                          <DoneIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    )}
                  </TableCell>
                </TableRow>
              ))}
              {payrolls && payrolls.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} align="center" sx={{ py: 4, color: "text.secondary" }}>
                    No payroll for {MONTHS[month - 1]} {year}. Use “Generate”.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Card>

      {/* Generate dialog */}
      <Dialog open={genOpen} onClose={() => setGenOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>Generate Payroll — {MONTHS[month - 1]} {year}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField select required label="Employee" value={gen.employeeId} onChange={(e) => setGen({ ...gen, employeeId: e.target.value })}>
              {employees?.items.map((emp) => <MenuItem key={emp.id} value={emp.id}>{emp.employeeCode} — {emp.fullName} ({formatMoney(emp.monthlyCtc)})</MenuItem>)}
            </TextField>
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="Working days" type="number" value={gen.workingDays} onChange={(e) => setGen({ ...gen, workingDays: Number(e.target.value) })} sx={{ flex: 1 }} />
              <TextField label="Present days" type="number" value={gen.presentDays} onChange={(e) => setGen({ ...gen, presentDays: Number(e.target.value) })} sx={{ flex: 1 }} />
            </Stack>
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="Bonus" type="number" value={gen.bonus} onChange={(e) => setGen({ ...gen, bonus: Number(e.target.value) })} sx={{ flex: 1 }} />
              <TextField label="Deductions" type="number" value={gen.deductions} onChange={(e) => setGen({ ...gen, deductions: Number(e.target.value) })} sx={{ flex: 1 }} />
            </Stack>
            <Alert severity="info">Outstanding advances are recovered automatically.</Alert>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setGenOpen(false)}>Cancel</Button>
          <Button variant="contained" disabled={!gen.employeeId || generate.isPending} onClick={handleGenerate}>
            {generate.isPending ? "Generating…" : "Generate"}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Advance dialog */}
      <Dialog open={advOpen} onClose={() => setAdvOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>Record Salary Advance</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField select required label="Employee" value={adv.employeeId} onChange={(e) => setAdv({ ...adv, employeeId: e.target.value })}>
              {employees?.items.map((emp) => <MenuItem key={emp.id} value={emp.id}>{emp.employeeCode} — {emp.fullName}</MenuItem>)}
            </TextField>
            <TextField label="Amount" type="number" value={adv.amount} onChange={(e) => setAdv({ ...adv, amount: Number(e.target.value) })} />
            <TextField label="Date" type="date" value={adv.advanceDate} onChange={(e) => setAdv({ ...adv, advanceDate: e.target.value })} InputLabelProps={{ shrink: true }} />
            <TextField label="Notes" value={adv.notes} onChange={(e) => setAdv({ ...adv, notes: e.target.value })} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAdvOpen(false)}>Cancel</Button>
          <Button variant="contained" disabled={!adv.employeeId || adv.amount <= 0 || createAdvance.isPending} onClick={handleAdvance}>
            {createAdvance.isPending ? "Saving…" : "Record advance"}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
