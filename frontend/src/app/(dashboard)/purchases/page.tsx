"use client";

import { useState } from "react";
import { AxiosError } from "axios";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Card from "@mui/material/Card";
import Button from "@mui/material/Button";
import IconButton from "@mui/material/IconButton";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import MenuItem from "@mui/material/MenuItem";
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
import DeleteIcon from "@mui/icons-material/Delete";
import { usePurchases, useCreatePurchase } from "@/features/purchases/hooks";
import { useSuppliers } from "@/features/suppliers/hooks";
import { useStockLevels } from "@/features/inventory/hooks";
import { formatMoney } from "@/lib/config";
import { useEffectiveStoreId } from "@/store/storeStore";

interface LineForm {
  variantId: string;
  quantity: number;
  unitCost: number;
  gstRate: number;
}

const today = () => new Date().toISOString().slice(0, 10);

export default function PurchasesPage() {
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [supplierId, setSupplierId] = useState("");
  const [invoiceNumber, setInvoiceNumber] = useState("");
  const [invoiceDate, setInvoiceDate] = useState(today());
  const [amountPaid, setAmountPaid] = useState(0);
  const [lines, setLines] = useState<LineForm[]>([{ variantId: "", quantity: 1, unitCost: 0, gstRate: 12 }]);

  const storeId = useEffectiveStoreId();
  const { data, isFetching } = usePurchases({ page: page + 1, pageSize });
  const { data: suppliers } = useSuppliers({ page: 1, pageSize: 100 });
  const { data: variants } = useStockLevels(storeId, { page: 1, pageSize: 200 });
  const createPurchase = useCreatePurchase();

  const subtotal = lines.reduce((sum, l) => sum + l.quantity * l.unitCost, 0);
  const tax = lines.reduce((sum, l) => sum + (l.quantity * l.unitCost * l.gstRate) / 100, 0);
  const grandTotal = subtotal + tax;

  const resetForm = () => {
    setSupplierId("");
    setInvoiceNumber("");
    setInvoiceDate(today());
    setAmountPaid(0);
    setLines([{ variantId: "", quantity: 1, unitCost: 0, gstRate: 12 }]);
  };

  const handleCreate = async () => {
    setError(null);
    try {
      await createPurchase.mutateAsync({
        supplierId,
        storeId,
        invoiceNumber: invoiceNumber || null,
        invoiceDate,
        discount: 0,
        amountPaid: Number(amountPaid) || 0,
        items: lines.map((l) => ({
          variantId: l.variantId,
          quantity: Number(l.quantity),
          unitCost: Number(l.unitCost),
          gstRate: Number(l.gstRate),
        })),
      });
      setOpen(false);
      resetForm();
    } catch (err) {
      const e = err as AxiosError<{ title?: string }>;
      setError(e.response?.data?.title ?? "Could not create purchase.");
    }
  };

  const updateLine = (i: number, patch: Partial<LineForm>) =>
    setLines((ls) => ls.map((l, idx) => (idx === i ? { ...l, ...patch } : l)));

  const canSubmit =
    !!supplierId && lines.length > 0 && lines.every((l) => l.variantId && l.quantity > 0) && !createPurchase.isPending;

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h1">Purchases</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setOpen(true)}>
          New Purchase
        </Button>
      </Stack>

      <Card elevation={2}>
        {isFetching && <LinearProgress />}
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Supplier</TableCell>
                <TableCell>Invoice #</TableCell>
                <TableCell>Date</TableCell>
                <TableCell align="right">Grand Total</TableCell>
                <TableCell align="right">Paid</TableCell>
                <TableCell align="center">Payment</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data?.items.map((p) => (
                <TableRow key={p.id} hover>
                  <TableCell>{p.supplierName}</TableCell>
                  <TableCell>{p.invoiceNumber ?? "—"}</TableCell>
                  <TableCell>{p.invoiceDate}</TableCell>
                  <TableCell align="right">{formatMoney(p.grandTotal)}</TableCell>
                  <TableCell align="right">{formatMoney(p.amountPaid)}</TableCell>
                  <TableCell align="center">
                    <Chip
                      size="small"
                      color={p.paymentStatus === "Paid" ? "success" : p.paymentStatus === "PartiallyPaid" ? "warning" : "default"}
                      label={p.paymentStatus}
                    />
                  </TableCell>
                </TableRow>
              ))}
              {data && data.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} align="center" sx={{ py: 4, color: "text.secondary" }}>
                    No purchases yet.
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

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="md">
        <DialogTitle>New Purchase (Goods Receipt)</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}

            <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
              <TextField
                select required label="Supplier" value={supplierId}
                onChange={(e) => setSupplierId(e.target.value)} sx={{ flex: 1 }}
              >
                {(suppliers?.items ?? []).map((s) => (
                  <MenuItem key={s.id} value={s.id}>{s.code} — {s.name}</MenuItem>
                ))}
              </TextField>
              <TextField label="Invoice #" value={invoiceNumber} onChange={(e) => setInvoiceNumber(e.target.value)} />
              <TextField label="Invoice date" type="date" value={invoiceDate} onChange={(e) => setInvoiceDate(e.target.value)} InputLabelProps={{ shrink: true }} />
            </Stack>

            <Divider>Line items</Divider>

            {lines.map((line, i) => (
              <Stack key={i} direction={{ xs: "column", sm: "row" }} spacing={1} alignItems="center">
                <TextField
                  select label="Variant" value={line.variantId}
                  onChange={(e) => updateLine(i, { variantId: e.target.value })} sx={{ flex: 2, minWidth: 200 }}
                >
                  {(variants?.items ?? []).map((v) => (
                    <MenuItem key={v.variantId} value={v.variantId}>
                      {v.sku} — {v.productName}
                    </MenuItem>
                  ))}
                </TextField>
                <TextField label="Qty" type="number" value={line.quantity} onChange={(e) => updateLine(i, { quantity: Number(e.target.value) })} sx={{ width: 90 }} />
                <TextField label="Unit cost" type="number" value={line.unitCost} onChange={(e) => updateLine(i, { unitCost: Number(e.target.value) })} sx={{ width: 120 }} />
                <TextField label="GST %" type="number" value={line.gstRate} onChange={(e) => updateLine(i, { gstRate: Number(e.target.value) })} sx={{ width: 90 }} />
                <IconButton color="error" disabled={lines.length === 1} onClick={() => setLines((ls) => ls.filter((_, idx) => idx !== i))}>
                  <DeleteIcon />
                </IconButton>
              </Stack>
            ))}

            <Button startIcon={<AddIcon />} onClick={() => setLines((ls) => [...ls, { variantId: "", quantity: 1, unitCost: 0, gstRate: 12 }])} sx={{ alignSelf: "flex-start" }}>
              Add line
            </Button>

            <Divider />

            <Stack direction="row" justifyContent="flex-end" spacing={4}>
              <Box>
                <Typography variant="body2" color="text.secondary">Subtotal: {formatMoney(subtotal)}</Typography>
                <Typography variant="body2" color="text.secondary">GST: {formatMoney(tax)}</Typography>
                <Typography variant="h6">Grand total: {formatMoney(grandTotal)}</Typography>
              </Box>
            </Stack>

            <TextField
              label="Amount paid now" type="number" value={amountPaid}
              onChange={(e) => setAmountPaid(Number(e.target.value))} sx={{ width: 200, alignSelf: "flex-end" }}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button variant="contained" disabled={!canSubmit} onClick={handleCreate}>
            {createPurchase.isPending ? "Saving…" : "Create purchase"}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
