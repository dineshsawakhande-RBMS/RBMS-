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
import LoyaltyIcon from "@mui/icons-material/Loyalty";
import { useCustomers, useCreateCustomer, useDeleteCustomer } from "@/features/customers/hooks";
import { useToast } from "@/components/providers/ToastProvider";
import ConfirmDialog from "@/components/common/ConfirmDialog";

export default function CustomersPage() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState({ name: "", mobile: "", email: "", city: "" });

  const [deleteTarget, setDeleteTarget] = useState<{ id: string; name: string } | null>(null);
  const toast = useToast();

  const { data, isFetching } = useCustomers({ search: search || undefined, page: page + 1, pageSize });
  const createCustomer = useCreateCustomer();
  const deleteCustomer = useDeleteCustomer();

  const handleDelete = async () => {
    if (!deleteTarget) return;
    try {
      await deleteCustomer.mutateAsync(deleteTarget.id);
      toast(`${deleteTarget.name} deleted`, "info");
    } catch {
      toast("Could not delete customer", "error");
    } finally {
      setDeleteTarget(null);
    }
  };

  const handleCreate = async () => {
    setError(null);
    try {
      await createCustomer.mutateAsync({
        name: form.name.trim(),
        mobile: form.mobile.trim(),
        email: form.email || null,
        city: form.city || null,
      });
      setOpen(false);
      setForm({ name: "", mobile: "", email: "", city: "" });
    } catch (err) {
      const e = err as AxiosError<{ title?: string }>;
      setError(e.response?.status === 409 ? "A customer with that mobile already exists." : (e.response?.data?.title ?? "Could not create customer."));
    }
  };

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h1">Customers</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setOpen(true)}>
          New Customer
        </Button>
      </Stack>

      <Card elevation={0}>
        <Box sx={{ p: 2 }}>
          <TextField
            size="small"
            label="Search name or mobile"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(0); }}
            sx={{ width: { xs: "100%", tablet: 320 } }}
          />
        </Box>
        {isFetching && <LinearProgress />}
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Mobile</TableCell>
                <TableCell>Email</TableCell>
                <TableCell align="right">Loyalty Points</TableCell>
                <TableCell align="center">Status</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data?.items.map((c) => (
                <TableRow key={c.id} hover>
                  <TableCell>{c.name}</TableCell>
                  <TableCell>{c.mobile}</TableCell>
                  <TableCell>{c.email ?? "—"}</TableCell>
                  <TableCell align="right">
                    <Chip size="small" icon={<LoyaltyIcon />} color={c.loyaltyPoints > 0 ? "secondary" : "default"} label={c.loyaltyPoints} />
                  </TableCell>
                  <TableCell align="center">
                    <Chip size="small" color={c.isActive ? "success" : "default"} label={c.isActive ? "Active" : "Inactive"} />
                  </TableCell>
                  <TableCell align="right">
                    <Tooltip title="Delete">
                      <IconButton size="small" color="error" onClick={() => setDeleteTarget({ id: c.id, name: c.name })}>
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
              {data && data.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} align="center" sx={{ py: 4, color: "text.secondary" }}>
                    No customers yet.
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
        <DialogTitle>New Customer</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField label="Name" required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
            <TextField label="Mobile" required value={form.mobile} onChange={(e) => setForm({ ...form, mobile: e.target.value })} />
            <TextField label="Email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
            <TextField label="City" value={form.city} onChange={(e) => setForm({ ...form, city: e.target.value })} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button variant="contained" disabled={!form.name || !form.mobile || createCustomer.isPending} onClick={handleCreate}>
            {createCustomer.isPending ? "Saving…" : "Create"}
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={!!deleteTarget}
        title="Delete customer"
        message={`Delete ${deleteTarget?.name ?? "this customer"}? They'll be hidden but kept for sales history.`}
        loading={deleteCustomer.isPending}
        onConfirm={handleDelete}
        onClose={() => setDeleteTarget(null)}
      />
    </Box>
  );
}
