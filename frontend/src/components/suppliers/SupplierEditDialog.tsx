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
import { useSupplier, useUpdateSupplier } from "@/features/suppliers/hooks";
import { useToast } from "@/components/providers/ToastProvider";

export default function SupplierEditDialog({ supplierId, onClose }: { supplierId: string | null; onClose: () => void }) {
  const { data, isLoading } = useSupplier(supplierId);
  const update = useUpdateSupplier();
  const toast = useToast();
  const [form, setForm] = useState({ name: "", gstin: "", contactPerson: "", phone: "", email: "", city: "", paymentTermsDays: 0, isActive: true });
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (data) {
      setForm({
        name: data.name, gstin: data.gstin ?? "", contactPerson: data.contactPerson ?? "",
        phone: data.phone ?? "", email: data.email ?? "", city: data.city ?? "",
        paymentTermsDays: data.paymentTermsDays, isActive: data.isActive,
      });
      setError(null);
    }
  }, [data]);

  const set = (k: keyof typeof form, v: string | number | boolean) => setForm((f) => ({ ...f, [k]: v }));

  const save = async () => {
    if (!data) return;
    setError(null);
    try {
      await update.mutateAsync({
        id: data.id, name: form.name.trim(), gstin: form.gstin || null, contactPerson: form.contactPerson || null,
        phone: form.phone || null, email: form.email || null, city: form.city || null,
        paymentTermsDays: Number(form.paymentTermsDays), isActive: form.isActive,
      });
      toast("Supplier updated");
      onClose();
    } catch (err) {
      setError((err as AxiosError<{ title?: string }>).response?.data?.title ?? "Could not update supplier.");
    }
  };

  return (
    <Dialog open={!!supplierId} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Edit Supplier</DialogTitle>
      <DialogContent>
        {isLoading || !data ? (
          <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}><CircularProgress /></Box>
        ) : (
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField label="Code" value={data.code} disabled helperText="Supplier code can't be changed." />
            <TextField label="Name" required value={form.name} onChange={(e) => set("name", e.target.value)} />
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="GSTIN" value={form.gstin} onChange={(e) => set("gstin", e.target.value)} sx={{ flex: 1 }} />
              <TextField label="Contact person" value={form.contactPerson} onChange={(e) => set("contactPerson", e.target.value)} sx={{ flex: 1 }} />
            </Stack>
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="Phone" value={form.phone} onChange={(e) => set("phone", e.target.value)} sx={{ flex: 1 }} />
              <TextField label="Email" value={form.email} onChange={(e) => set("email", e.target.value)} sx={{ flex: 1 }} />
            </Stack>
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="City" value={form.city} onChange={(e) => set("city", e.target.value)} sx={{ flex: 1 }} />
              <TextField label="Payment terms (days)" type="number" value={form.paymentTermsDays} onChange={(e) => set("paymentTermsDays", Number(e.target.value))} sx={{ width: 180 }} />
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
