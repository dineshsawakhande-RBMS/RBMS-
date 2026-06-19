"use client";

import { useEffect, useState } from "react";
import { AxiosError } from "axios";
import Dialog from "@mui/material/Dialog";
import DialogTitle from "@mui/material/DialogTitle";
import DialogContent from "@mui/material/DialogContent";
import DialogActions from "@mui/material/DialogActions";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import MenuItem from "@mui/material/MenuItem";
import Divider from "@mui/material/Divider";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import { useEmployee, useUpdateEmployee } from "@/features/employees/hooks";
import { useToast } from "@/components/providers/ToastProvider";

const STATUSES = ["Active", "OnLeave", "Suspended", "Resigned", "Terminated"];

export default function EmployeeEditDialog({ employeeId, onClose }: { employeeId: string | null; onClose: () => void }) {
  const { data, isLoading } = useEmployee(employeeId);
  const update = useUpdateEmployee();
  const toast = useToast();
  const [form, setForm] = useState({
    fullName: "", mobile: "", email: "", designation: "", department: "",
    monthlyCtc: 0, status: "Active", exitDate: "", bankName: "", ifsc: "", accountLast4: "",
  });
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (data) {
      setForm({
        fullName: data.fullName, mobile: data.mobile, email: data.email ?? "",
        designation: data.designation ?? "", department: data.department ?? "",
        monthlyCtc: data.monthlyCtc, status: data.status, exitDate: data.exitDate ?? "",
        bankName: data.bankName ?? "", ifsc: data.ifsc ?? "", accountLast4: data.accountLast4 ?? "",
      });
      setError(null);
    }
  }, [data]);

  const set = (k: keyof typeof form, v: string | number) => setForm((f) => ({ ...f, [k]: v }));

  const save = async () => {
    if (!data) return;
    setError(null);
    try {
      await update.mutateAsync({
        id: data.id, fullName: form.fullName.trim(), mobile: form.mobile.trim(), email: form.email || null,
        designation: form.designation || null, department: form.department || null,
        monthlyCtc: Number(form.monthlyCtc), status: form.status, exitDate: form.exitDate || null,
        bankName: form.bankName || null, ifsc: form.ifsc || null, accountLast4: form.accountLast4 || null,
      });
      toast("Employee updated");
      onClose();
    } catch (err) {
      setError((err as AxiosError<{ title?: string }>).response?.data?.title ?? "Could not update employee.");
    }
  };

  return (
    <Dialog open={!!employeeId} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Edit Employee</DialogTitle>
      <DialogContent>
        {isLoading || !data ? (
          <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}><CircularProgress /></Box>
        ) : (
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField label="Code" value={data.employeeCode} disabled />
            <TextField label="Full name" required value={form.fullName} onChange={(e) => set("fullName", e.target.value)} />
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="Mobile" required value={form.mobile} onChange={(e) => set("mobile", e.target.value)} sx={{ flex: 1 }} />
              <TextField label="Email" value={form.email} onChange={(e) => set("email", e.target.value)} sx={{ flex: 1 }} />
            </Stack>
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="Designation" value={form.designation} onChange={(e) => set("designation", e.target.value)} sx={{ flex: 1 }} />
              <TextField label="Department" value={form.department} onChange={(e) => set("department", e.target.value)} sx={{ flex: 1 }} />
            </Stack>
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="Monthly CTC" type="number" value={form.monthlyCtc} onChange={(e) => set("monthlyCtc", Number(e.target.value))} sx={{ flex: 1 }} />
              <TextField select label="Status" value={form.status} onChange={(e) => set("status", e.target.value)} sx={{ flex: 1 }}>
                {STATUSES.map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
              </TextField>
              <TextField label="Exit date" type="date" value={form.exitDate} onChange={(e) => set("exitDate", e.target.value)} InputLabelProps={{ shrink: true }} sx={{ flex: 1 }} />
            </Stack>
            <Divider>Bank</Divider>
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="Bank name" value={form.bankName} onChange={(e) => set("bankName", e.target.value)} sx={{ flex: 2 }} />
              <TextField label="IFSC" value={form.ifsc} onChange={(e) => set("ifsc", e.target.value)} sx={{ flex: 1 }} />
              <TextField label="A/C last 4" value={form.accountLast4} onChange={(e) => set("accountLast4", e.target.value)} sx={{ width: 110 }} inputProps={{ maxLength: 4 }} />
            </Stack>
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" disabled={!form.fullName || !form.mobile || update.isPending} onClick={save}>
          {update.isPending ? "Saving…" : "Save changes"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
