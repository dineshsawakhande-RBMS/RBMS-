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
import IconButton from "@mui/material/IconButton";
import Tooltip from "@mui/material/Tooltip";
import Dialog from "@mui/material/Dialog";
import DialogTitle from "@mui/material/DialogTitle";
import DialogContent from "@mui/material/DialogContent";
import DialogActions from "@mui/material/DialogActions";
import Alert from "@mui/material/Alert";
import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import { useSuppliers, useCreateSupplier, useDeleteSupplier } from "@/features/suppliers/hooks";
import SupplierDetailDialog from "@/components/suppliers/SupplierDetailDialog";
import { useToast } from "@/components/providers/ToastProvider";
import ConfirmDialog from "@/components/common/ConfirmDialog";
import { formatMoney } from "@/lib/config";

export default function SuppliersPage() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState({ code: "", name: "", gstin: "", phone: "", paymentTermsDays: 30 });
  const [detailId, setDetailId] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<{ id: string; name: string } | null>(null);
  const toast = useToast();

  const { data, isFetching } = useSuppliers({ search: search || undefined, page: page + 1, pageSize });
  const createSupplier = useCreateSupplier();
  const deleteSupplier = useDeleteSupplier();

  const handleDelete = async () => {
    if (!deleteTarget) return;
    try {
      await deleteSupplier.mutateAsync(deleteTarget.id);
      toast(`${deleteTarget.name} deleted`, "info");
    } catch {
      toast("Could not delete supplier", "error");
    } finally {
      setDeleteTarget(null);
    }
  };

  const handleCreate = async () => {
    setError(null);
    try {
      await createSupplier.mutateAsync({
        code: form.code.trim(),
        name: form.name.trim(),
        gstin: form.gstin || null,
        phone: form.phone || null,
        paymentTermsDays: Number(form.paymentTermsDays) || 0,
        openingBalance: 0,
      });
      setOpen(false);
      setForm({ code: "", name: "", gstin: "", phone: "", paymentTermsDays: 30 });
    } catch (err) {
      const e = err as AxiosError<{ title?: string }>;
      setError(e.response?.status === 409 ? "A supplier with that code already exists." : (e.response?.data?.title ?? "Could not create supplier."));
    }
  };

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h1">Suppliers</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setOpen(true)}>
          New Supplier
        </Button>
      </Stack>

      <Card elevation={2}>
        <Box sx={{ p: 2 }}>
          <TextField
            size="small"
            label="Search suppliers"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(0); }}
            sx={{ width: { xs: "100%", sm: 320 } }}
          />
        </Box>
        {isFetching && <LinearProgress />}
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Code</TableCell>
                <TableCell>Name</TableCell>
                <TableCell>Phone</TableCell>
                <TableCell>GSTIN</TableCell>
                <TableCell align="right">Outstanding</TableCell>
                <TableCell align="center">Status</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data?.items.map((s) => (
                <TableRow key={s.id} hover sx={{ cursor: "pointer" }} onClick={() => setDetailId(s.id)}>
                  <TableCell>{s.code}</TableCell>
                  <TableCell>{s.name}</TableCell>
                  <TableCell>{s.phone ?? "—"}</TableCell>
                  <TableCell>{s.gstin ?? "—"}</TableCell>
                  <TableCell align="right">{formatMoney(s.outstandingBalance)}</TableCell>
                  <TableCell align="center">
                    <Chip size="small" color={s.isActive ? "success" : "default"} label={s.isActive ? "Active" : "Inactive"} />
                  </TableCell>
                  <TableCell align="right">
                    <Tooltip title="Delete">
                      <IconButton
                        size="small" color="error"
                        onClick={(ev) => { ev.stopPropagation(); setDeleteTarget({ id: s.id, name: s.name }); }}
                      >
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
              {data && data.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} align="center" sx={{ py: 4, color: "text.secondary" }}>
                    No suppliers yet.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
        <TablePagination
          component="div"
          count={data?.totalCount ?? 0}
          page={page}
          onPageChange={(_, p) => setPage(p)}
          rowsPerPage={pageSize}
          onRowsPerPageChange={(e) => { setPageSize(parseInt(e.target.value, 10)); setPage(0); }}
          rowsPerPageOptions={[10, 20, 50]}
        />
      </Card>

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>New Supplier</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField label="Code" required value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} />
            <TextField label="Name" required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
            <TextField label="GSTIN" value={form.gstin} onChange={(e) => setForm({ ...form, gstin: e.target.value })} />
            <TextField label="Phone" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
            <TextField label="Payment terms (days)" type="number" value={form.paymentTermsDays} onChange={(e) => setForm({ ...form, paymentTermsDays: Number(e.target.value) })} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            disabled={!form.code || !form.name || createSupplier.isPending}
            onClick={handleCreate}
          >
            {createSupplier.isPending ? "Saving…" : "Create"}
          </Button>
        </DialogActions>
      </Dialog>

      <SupplierDetailDialog supplierId={detailId} onClose={() => setDetailId(null)} />

      <ConfirmDialog
        open={!!deleteTarget}
        title="Delete supplier"
        message={`Delete ${deleteTarget?.name ?? "this supplier"}? They'll be hidden but kept for purchase history.`}
        loading={deleteSupplier.isPending}
        onConfirm={handleDelete}
        onClose={() => setDeleteTarget(null)}
      />
    </Box>
  );
}
