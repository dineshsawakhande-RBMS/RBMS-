"use client";

import { useState } from "react";
import { AxiosError } from "axios";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Card from "@mui/material/Card";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import TablePagination from "@mui/material/TablePagination";
import Chip from "@mui/material/Chip";
import LinearProgress from "@mui/material/LinearProgress";
import Dialog from "@mui/material/Dialog";
import DialogTitle from "@mui/material/DialogTitle";
import DialogContent from "@mui/material/DialogContent";
import DialogActions from "@mui/material/DialogActions";
import Alert from "@mui/material/Alert";
import Divider from "@mui/material/Divider";
import AddIcon from "@mui/icons-material/Add";
import { useEmployees, useCreateEmployee } from "@/features/employees/hooks";
import { useToast } from "@/components/providers/ToastProvider";
import { formatMoney } from "@/lib/config";

const today = () => new Date().toISOString().slice(0, 10);
const statusColor = (s: string) =>
  s === "Active" ? "success" : s === "OnLeave" ? "info" : s === "Suspended" ? "warning" : "default";

export default function EmployeesPage() {
  const toast = useToast();
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState({
    employeeCode: "", fullName: "", mobile: "", email: "", designation: "",
    department: "", joiningDate: today(), monthlyCtc: 0, bankName: "", ifsc: "", accountLast4: "",
  });

  const { data, isFetching } = useEmployees({ search: search || undefined, page: page + 1, pageSize });
  const createEmployee = useCreateEmployee();

  const set = (k: keyof typeof form, v: string | number) => setForm((f) => ({ ...f, [k]: v }));

  const handleCreate = async () => {
    setError(null);
    try {
      await createEmployee.mutateAsync({
        employeeCode: form.employeeCode.trim(),
        fullName: form.fullName.trim(),
        mobile: form.mobile.trim(),
        email: form.email || null,
        designation: form.designation || null,
        department: form.department || null,
        joiningDate: form.joiningDate,
        monthlyCtc: Number(form.monthlyCtc),
        bankName: form.bankName || null,
        ifsc: form.ifsc || null,
        accountLast4: form.accountLast4 || null,
      });
      toast(`${form.fullName} added`, "success");
      setOpen(false);
      setForm({ employeeCode: "", fullName: "", mobile: "", email: "", designation: "", department: "", joiningDate: today(), monthlyCtc: 0, bankName: "", ifsc: "", accountLast4: "" });
    } catch (err) {
      const e = err as AxiosError<{ title?: string }>;
      setError(e.response?.status === 409 ? "That employee code already exists." : (e.response?.data?.title ?? "Could not add employee."));
    }
  };

  const canSubmit = !!form.employeeCode.trim() && !!form.fullName.trim() && !!form.mobile.trim() && !createEmployee.isPending;

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h1">Employees</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setOpen(true)}>
          New Employee
        </Button>
      </Stack>

      <Card elevation={0}>
        <Box sx={{ p: 2 }}>
          <TextField
            size="small" label="Search name, code or mobile" value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(0); }}
            sx={{ width: { xs: "100%", tablet: 340 } }}
          />
        </Box>
        {isFetching && <LinearProgress />}
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Code</TableCell>
                <TableCell>Name</TableCell>
                <TableCell>Designation</TableCell>
                <TableCell>Mobile</TableCell>
                <TableCell align="right">Monthly CTC</TableCell>
                <TableCell align="center">Status</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data?.items.map((e) => (
                <TableRow key={e.id} hover>
                  <TableCell>{e.employeeCode}</TableCell>
                  <TableCell>{e.fullName}</TableCell>
                  <TableCell>{e.designation ?? "—"}</TableCell>
                  <TableCell>{e.mobile}</TableCell>
                  <TableCell align="right">{formatMoney(e.monthlyCtc)}</TableCell>
                  <TableCell align="center"><Chip size="small" color={statusColor(e.status)} label={e.status} /></TableCell>
                </TableRow>
              ))}
              {data && data.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} align="center" sx={{ py: 4, color: "text.secondary" }}>
                    No employees yet.
                  </TableCell>
                </TableRow>
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
      </Card>

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>New Employee</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="Employee code" required value={form.employeeCode} onChange={(e) => set("employeeCode", e.target.value)} sx={{ flex: 1 }} />
              <TextField label="Full name" required value={form.fullName} onChange={(e) => set("fullName", e.target.value)} sx={{ flex: 2 }} />
            </Stack>
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="Mobile" required value={form.mobile} onChange={(e) => set("mobile", e.target.value)} sx={{ flex: 1 }} />
              <TextField label="Email" value={form.email} onChange={(e) => set("email", e.target.value)} sx={{ flex: 1 }} />
            </Stack>
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="Designation" value={form.designation} onChange={(e) => set("designation", e.target.value)} sx={{ flex: 1 }} />
              <TextField label="Department" value={form.department} onChange={(e) => set("department", e.target.value)} sx={{ flex: 1 }} />
            </Stack>
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="Joining date" type="date" value={form.joiningDate} onChange={(e) => set("joiningDate", e.target.value)} InputLabelProps={{ shrink: true }} sx={{ flex: 1 }} />
              <TextField label="Monthly CTC" type="number" value={form.monthlyCtc} onChange={(e) => set("monthlyCtc", Number(e.target.value))} sx={{ flex: 1 }} />
            </Stack>
            <Divider>Bank (optional)</Divider>
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="Bank name" value={form.bankName} onChange={(e) => set("bankName", e.target.value)} sx={{ flex: 2 }} />
              <TextField label="IFSC" value={form.ifsc} onChange={(e) => set("ifsc", e.target.value)} sx={{ flex: 1 }} />
              <TextField label="A/C last 4" value={form.accountLast4} onChange={(e) => set("accountLast4", e.target.value)} sx={{ width: 110 }} inputProps={{ maxLength: 4 }} />
            </Stack>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button variant="contained" disabled={!canSubmit} onClick={handleCreate}>
            {createEmployee.isPending ? "Saving…" : "Add employee"}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
