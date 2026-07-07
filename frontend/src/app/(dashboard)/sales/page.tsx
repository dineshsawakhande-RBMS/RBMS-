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
import Tooltip from "@mui/material/Tooltip";
import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import AssignmentReturnIcon from "@mui/icons-material/AssignmentReturn";
import ReceiptIcon from "@mui/icons-material/Receipt";
import { useSales, useCreateSale, downloadInvoice } from "@/features/sales/hooks";
import { useStockLevels } from "@/features/inventory/hooks";
import { useCustomers } from "@/features/customers/hooks";
import SaleReturnDialog from "@/components/sales/SaleReturnDialog";
import { formatMoney } from "@/lib/config";
import { useEffectiveStoreId } from "@/store/storeStore";
import type { PaymentMethod } from "@/types";

interface LineForm {
  variantId: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  gstRate: number;
}

const PAYMENT_METHODS: PaymentMethod[] = ["Cash", "Card", "UPI", "BankTransfer", "Wallet", "Cheque"];

export default function SalesPage() {
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [method, setMethod] = useState<PaymentMethod>("Cash");
  const [customerId, setCustomerId] = useState("");
  const [returnSaleId, setReturnSaleId] = useState<string | null>(null);
  const [lines, setLines] = useState<LineForm[]>([{ variantId: "", quantity: 1, unitPrice: 0, discount: 0, gstRate: 12 }]);

  const storeId = useEffectiveStoreId();
  const { data, isFetching } = useSales({ page: page + 1, pageSize });
  const { data: variants } = useStockLevels(storeId, { page: 1, pageSize: 200 });
  const { data: customers } = useCustomers({ page: 1, pageSize: 100 });
  const createSale = useCreateSale();

  const subtotal = lines.reduce((s, l) => s + Math.max(0, l.quantity * l.unitPrice - l.discount), 0);
  const tax = lines.reduce((s, l) => s + (Math.max(0, l.quantity * l.unitPrice - l.discount) * l.gstRate) / 100, 0);
  const grandTotal = Math.round(subtotal + tax);

  const reset = () => {
    setMethod("Cash");
    setCustomerId("");
    setLines([{ variantId: "", quantity: 1, unitPrice: 0, discount: 0, gstRate: 12 }]);
  };

  const updateLine = (i: number, patch: Partial<LineForm>) =>
    setLines((ls) => ls.map((l, idx) => (idx === i ? { ...l, ...patch } : l)));

  const onVariantChange = (i: number, variantId: string) => {
    const v = variants?.items.find((x) => x.variantId === variantId);
    updateLine(i, { variantId, unitPrice: v ? v.sellingPrice : 0 });
  };

  const handleCreate = async () => {
    setError(null);
    try {
      await createSale.mutateAsync({
        storeId,
        customerId: customerId || null,
        discount: 0,
        items: lines.map((l) => ({
          variantId: l.variantId,
          quantity: Number(l.quantity),
          unitPrice: Number(l.unitPrice),
          discount: Number(l.discount),
          gstRate: Number(l.gstRate),
        })),
        payments: grandTotal > 0 ? [{ method, amount: grandTotal }] : [],
      });
      setOpen(false);
      reset();
    } catch (err) {
      const e = err as AxiosError<{ title?: string }>;
      setError(
        e.response?.status === 409
          ? "Not enough stock for one or more items."
          : (e.response?.data?.title ?? "Could not complete the sale."),
      );
    }
  };

  const canSubmit = lines.length > 0 && lines.every((l) => l.variantId && l.quantity > 0) && !createSale.isPending;

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h1">Sales</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setOpen(true)}>
          New Sale
        </Button>
      </Stack>

      <Card elevation={2}>
        {isFetching && <LinearProgress />}
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Invoice #</TableCell>
                <TableCell>Date</TableCell>
                <TableCell align="right">Total</TableCell>
                <TableCell align="center">Status</TableCell>
                <TableCell align="center">Payment</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data?.items.map((s) => (
                <TableRow key={s.id} hover>
                  <TableCell>{s.invoiceNumber}</TableCell>
                  <TableCell>{new Date(s.invoiceDate).toLocaleString("en-IN")}</TableCell>
                  <TableCell align="right">{formatMoney(s.grandTotal)}</TableCell>
                  <TableCell align="center"><Chip size="small" label={s.status} /></TableCell>
                  <TableCell align="center">
                    <Chip size="small" color={s.paymentStatus === "Paid" ? "success" : "warning"} label={s.paymentStatus} />
                  </TableCell>
                  <TableCell align="right">
                    <Tooltip title="Download invoice (PDF)">
                      <IconButton size="small" onClick={() => downloadInvoice(s.id, s.invoiceNumber)}>
                        <ReceiptIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Process return">
                      <IconButton size="small" onClick={() => setReturnSaleId(s.id)}>
                        <AssignmentReturnIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
              {data && data.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} align="center" sx={{ py: 4, color: "text.secondary" }}>
                    No sales yet.
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
        <DialogTitle>New Sale (POS)</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}

            <TextField select label="Customer (optional)" value={customerId} onChange={(e) => setCustomerId(e.target.value)} sx={{ maxWidth: 360 }}>
              <MenuItem value="">Walk-in customer</MenuItem>
              {customers?.items.map((c) => (
                <MenuItem key={c.id} value={c.id}>{c.name} — {c.mobile}</MenuItem>
              ))}
            </TextField>

            <Divider>Items</Divider>
            {lines.map((line, i) => (
              <Stack key={i} direction={{ xs: "column", sm: "row" }} spacing={1} alignItems="center">
                <TextField
                  select label="Item" value={line.variantId}
                  onChange={(e) => onVariantChange(i, e.target.value)} sx={{ flex: 2, minWidth: 220 }}
                >
                  {variants?.items.map((v) => (
                    <MenuItem key={v.variantId} value={v.variantId}>
                      {v.sku} — {v.productName} ({v.quantityOnHand} in stock)
                    </MenuItem>
                  ))}
                </TextField>
                <TextField label="Qty" type="number" value={line.quantity} onChange={(e) => updateLine(i, { quantity: Number(e.target.value) })} sx={{ width: 90 }} />
                <TextField label="Price" type="number" value={line.unitPrice} onChange={(e) => updateLine(i, { unitPrice: Number(e.target.value) })} sx={{ width: 110 }} />
                <TextField label="Disc" type="number" value={line.discount} onChange={(e) => updateLine(i, { discount: Number(e.target.value) })} sx={{ width: 90 }} />
                <TextField label="GST %" type="number" value={line.gstRate} onChange={(e) => updateLine(i, { gstRate: Number(e.target.value) })} sx={{ width: 90 }} />
                <IconButton color="error" disabled={lines.length === 1} onClick={() => setLines((ls) => ls.filter((_, idx) => idx !== i))}>
                  <DeleteIcon />
                </IconButton>
              </Stack>
            ))}
            <Button startIcon={<AddIcon />} onClick={() => setLines((ls) => [...ls, { variantId: "", quantity: 1, unitPrice: 0, discount: 0, gstRate: 12 }])} sx={{ alignSelf: "flex-start" }}>
              Add item
            </Button>

            <Divider />
            <Stack direction="row" justifyContent="space-between" alignItems="center">
              <TextField select label="Payment method" value={method} onChange={(e) => setMethod(e.target.value as PaymentMethod)} sx={{ width: 200 }}>
                {PAYMENT_METHODS.map((m) => <MenuItem key={m} value={m}>{m}</MenuItem>)}
              </TextField>
              <Box sx={{ textAlign: "right" }}>
                <Typography variant="body2" color="text.secondary">Subtotal: {formatMoney(subtotal)}</Typography>
                <Typography variant="body2" color="text.secondary">GST: {formatMoney(tax)}</Typography>
                <Typography variant="h6">Total: {formatMoney(grandTotal)}</Typography>
              </Box>
            </Stack>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button variant="contained" disabled={!canSubmit} onClick={handleCreate}>
            {createSale.isPending ? "Saving…" : "Complete sale"}
          </Button>
        </DialogActions>
      </Dialog>

      <SaleReturnDialog saleId={returnSaleId} onClose={() => setReturnSaleId(null)} />
    </Box>
  );
}
