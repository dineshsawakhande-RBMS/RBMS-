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
import FormControlLabel from "@mui/material/FormControlLabel";
import Switch from "@mui/material/Switch";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import { useCustomer, useUpdateCustomer } from "@/features/customers/hooks";
import { useToast } from "@/components/providers/ToastProvider";

export default function CustomerEditDialog({ customerId, onClose }: { customerId: string | null; onClose: () => void }) {
  const { data, isLoading } = useCustomer(customerId);
  const update = useUpdateCustomer();
  const toast = useToast();
  const [form, setForm] = useState({ name: "", email: "", addressLine1: "", city: "", state: "", pincode: "", isActive: true });
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (data) {
      setForm({
        name: data.name, email: data.email ?? "", addressLine1: data.addressLine1 ?? "",
        city: data.city ?? "", state: data.state ?? "", pincode: data.pincode ?? "", isActive: data.isActive,
      });
      setError(null);
    }
  }, [data]);

  const set = (k: keyof typeof form, v: string | boolean) => setForm((f) => ({ ...f, [k]: v }));

  const save = async () => {
    if (!data) return;
    setError(null);
    try {
      await update.mutateAsync({
        id: data.id, name: form.name.trim(), email: form.email || null,
        addressLine1: form.addressLine1 || null, city: form.city || null, state: form.state || null,
        pincode: form.pincode || null, isActive: form.isActive,
      });
      toast("Customer updated");
      onClose();
    } catch (err) {
      setError((err as AxiosError<{ title?: string }>).response?.data?.title ?? "Could not update customer.");
    }
  };

  return (
    <Dialog open={!!customerId} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Edit Customer</DialogTitle>
      <DialogContent>
        {isLoading || !data ? (
          <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}><CircularProgress /></Box>
        ) : (
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField label="Name" required value={form.name} onChange={(e) => set("name", e.target.value)} />
            <TextField label="Mobile" value={data.mobile} disabled helperText="Mobile is the unique key and can't be changed here." />
            <TextField label="Email" value={form.email} onChange={(e) => set("email", e.target.value)} />
            <TextField label="Address" value={form.addressLine1} onChange={(e) => set("addressLine1", e.target.value)} />
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="City" value={form.city} onChange={(e) => set("city", e.target.value)} sx={{ flex: 1 }} />
              <TextField label="State" value={form.state} onChange={(e) => set("state", e.target.value)} sx={{ flex: 1 }} />
              <TextField label="Pincode" value={form.pincode} onChange={(e) => set("pincode", e.target.value)} sx={{ width: 120 }} />
            </Stack>
            <FormControlLabel control={<Switch checked={form.isActive} onChange={(e) => set("isActive", e.target.checked)} />} label="Active" />
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" disabled={!form.name || update.isPending} onClick={save}>
          {update.isPending ? "Saving…" : "Save changes"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
