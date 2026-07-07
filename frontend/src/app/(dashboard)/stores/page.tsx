"use client";

import { useEffect, useState } from "react";
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
import Chip from "@mui/material/Chip";
import LinearProgress from "@mui/material/LinearProgress";
import IconButton from "@mui/material/IconButton";
import Tooltip from "@mui/material/Tooltip";
import Dialog from "@mui/material/Dialog";
import DialogTitle from "@mui/material/DialogTitle";
import DialogContent from "@mui/material/DialogContent";
import DialogActions from "@mui/material/DialogActions";
import Alert from "@mui/material/Alert";
import FormControlLabel from "@mui/material/FormControlLabel";
import Switch from "@mui/material/Switch";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import { useStores, useStore, useCreateStore, useUpdateStore, useDeleteStore } from "@/features/stores/hooks";
import { useToast } from "@/components/providers/ToastProvider";
import ConfirmDialog from "@/components/common/ConfirmDialog";

const emptyForm = {
  code: "", name: "", gstin: "", phone: "", email: "",
  addressLine1: "", city: "", state: "", pincode: "", isActive: true,
};

export default function StoresPage() {
  const toast = useToast();
  const { data: stores, isFetching } = useStores();
  const createStore = useCreateStore();
  const updateStore = useUpdateStore();
  const deleteStore = useDeleteStore();

  const [open, setOpen] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [deleteTarget, setDeleteTarget] = useState<{ id: string; name: string } | null>(null);

  const { data: editing } = useStore(editId);

  useEffect(() => {
    if (editing) {
      setForm({
        code: editing.code, name: editing.name, gstin: editing.gstin ?? "", phone: editing.phone ?? "",
        email: editing.email ?? "", addressLine1: editing.addressLine1 ?? "", city: editing.city ?? "",
        state: editing.state ?? "", pincode: editing.pincode ?? "", isActive: editing.isActive,
      });
      setError(null);
    }
  }, [editing]);

  const set = (k: keyof typeof form, v: string | boolean) => setForm((f) => ({ ...f, [k]: v }));

  const openCreate = () => { setEditId(null); setForm(emptyForm); setError(null); setOpen(true); };
  const openEdit = (id: string) => { setEditId(id); setOpen(true); };
  const close = () => { setOpen(false); setEditId(null); setForm(emptyForm); };

  const save = async () => {
    setError(null);
    try {
      if (editId) {
        await updateStore.mutateAsync({
          id: editId, name: form.name.trim(), gstin: form.gstin || null, phone: form.phone || null,
          email: form.email || null, addressLine1: form.addressLine1 || null, city: form.city || null,
          state: form.state || null, pincode: form.pincode || null, isActive: form.isActive,
        });
        toast("Store updated");
      } else {
        await createStore.mutateAsync({
          code: form.code.trim(), name: form.name.trim(), gstin: form.gstin || null, phone: form.phone || null,
          email: form.email || null, addressLine1: form.addressLine1 || null, city: form.city || null,
          state: form.state || null, pincode: form.pincode || null,
        });
        toast(`${form.name} added`, "success");
      }
      close();
    } catch (err) {
      const e = err as AxiosError<{ title?: string }>;
      setError(e.response?.status === 409 ? "That store code already exists." : (e.response?.data?.title ?? "Could not save store."));
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    try {
      await deleteStore.mutateAsync(deleteTarget.id);
      toast(`${deleteTarget.name} deleted`, "info");
    } catch {
      toast("Could not delete store", "error");
    } finally {
      setDeleteTarget(null);
    }
  };

  const saving = createStore.isPending || updateStore.isPending;
  const canSubmit = !!form.name.trim() && (!!editId || !!form.code.trim()) && !saving;

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h1">Stores</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>New Store</Button>
      </Stack>

      <Card elevation={0}>
        {isFetching && <LinearProgress />}
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Code</TableCell>
                <TableCell>Name</TableCell>
                <TableCell>City</TableCell>
                <TableCell>Phone</TableCell>
                <TableCell align="center">Status</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {stores?.map((s) => (
                <TableRow key={s.id} hover>
                  <TableCell>{s.code}</TableCell>
                  <TableCell>{s.name}</TableCell>
                  <TableCell>{s.city ?? "—"}</TableCell>
                  <TableCell>{s.phone ?? "—"}</TableCell>
                  <TableCell align="center">
                    <Chip size="small" color={s.isActive ? "success" : "default"} label={s.isActive ? "Active" : "Inactive"} />
                  </TableCell>
                  <TableCell align="right">
                    <Tooltip title="Edit"><IconButton size="small" onClick={() => openEdit(s.id)}><EditIcon fontSize="small" /></IconButton></Tooltip>
                    <Tooltip title="Delete"><IconButton size="small" color="error" onClick={() => setDeleteTarget({ id: s.id, name: s.name })}><DeleteIcon fontSize="small" /></IconButton></Tooltip>
                  </TableCell>
                </TableRow>
              ))}
              {stores && stores.length === 0 && (
                <TableRow><TableCell colSpan={6} align="center" sx={{ py: 4, color: "text.secondary" }}>No stores yet.</TableCell></TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Card>

      <Dialog open={open} onClose={close} fullWidth maxWidth="sm">
        <DialogTitle>{editId ? "Edit Store" : "New Store"}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="Code" required value={form.code} onChange={(e) => set("code", e.target.value)} disabled={!!editId} sx={{ flex: 1 }} />
              <TextField label="Name" required value={form.name} onChange={(e) => set("name", e.target.value)} sx={{ flex: 2 }} />
            </Stack>
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="GSTIN" value={form.gstin} onChange={(e) => set("gstin", e.target.value)} sx={{ flex: 1 }} />
              <TextField label="Phone" value={form.phone} onChange={(e) => set("phone", e.target.value)} sx={{ flex: 1 }} />
              <TextField label="Email" value={form.email} onChange={(e) => set("email", e.target.value)} sx={{ flex: 1 }} />
            </Stack>
            <TextField label="Address" value={form.addressLine1} onChange={(e) => set("addressLine1", e.target.value)} />
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="City" value={form.city} onChange={(e) => set("city", e.target.value)} sx={{ flex: 1 }} />
              <TextField label="State" value={form.state} onChange={(e) => set("state", e.target.value)} sx={{ flex: 1 }} />
              <TextField label="Pincode" value={form.pincode} onChange={(e) => set("pincode", e.target.value)} sx={{ flex: 1 }} />
            </Stack>
            {editId && (
              <FormControlLabel
                control={<Switch checked={form.isActive} onChange={(e) => set("isActive", e.target.checked)} />}
                label="Active"
              />
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={close}>Cancel</Button>
          <Button variant="contained" disabled={!canSubmit} onClick={save}>
            {saving ? "Saving…" : editId ? "Save changes" : "Add store"}
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={!!deleteTarget}
        title="Delete store"
        message={`Delete ${deleteTarget?.name ?? "this store"}? It'll be hidden from lists but kept for audit.`}
        loading={deleteStore.isPending}
        onConfirm={handleDelete}
        onClose={() => setDeleteTarget(null)}
      />
    </Box>
  );
}
