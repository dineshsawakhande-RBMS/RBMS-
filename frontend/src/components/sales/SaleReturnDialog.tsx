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
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Alert from "@mui/material/Alert";
import CircularProgress from "@mui/material/CircularProgress";
import Box from "@mui/material/Box";
import { useSale, useCreateSaleReturn } from "@/features/sales/hooks";
import { formatMoney } from "@/lib/config";
import type { PaymentMethod } from "@/types";

const METHODS: PaymentMethod[] = ["Cash", "Card", "UPI", "BankTransfer", "Wallet", "StoreCredit"];

export default function SaleReturnDialog({ saleId, onClose }: { saleId: string | null; onClose: () => void }) {
  const { data: sale, isLoading } = useSale(saleId);
  const createReturn = useCreateSaleReturn();
  const [qty, setQty] = useState<Record<string, number>>({});
  const [reason, setReason] = useState("");
  const [refundMethod, setRefundMethod] = useState<PaymentMethod>("Cash");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setQty({}); setReason(""); setRefundMethod("Cash"); setError(null);
  }, [saleId]);

  const lines = (sale?.items ?? [])
    .filter((it) => (qty[it.variantId] ?? 0) > 0)
    .map((it) => ({ variantId: it.variantId, quantity: qty[it.variantId]!, unitPrice: it.unitPrice }));
  const refundTotal = lines.reduce((s, l) => s + l.quantity * l.unitPrice, 0);

  const handleSubmit = async () => {
    if (!sale || lines.length === 0) return;
    setError(null);
    try {
      await createReturn.mutateAsync({
        saleId: sale.id,
        storeId: sale.storeId,
        reason: reason || null,
        refundMethod,
        items: lines,
      });
      onClose();
    } catch (err) {
      const e = err as AxiosError<{ title?: string }>;
      setError(e.response?.data?.title ?? "Could not process the return.");
    }
  };

  return (
    <Dialog open={!!saleId} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>Return — {sale?.invoiceNumber ?? ""}</DialogTitle>
      <DialogContent>
        {isLoading || !sale ? (
          <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}><CircularProgress /></Box>
        ) : (
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Item</TableCell>
                  <TableCell align="right">Sold</TableCell>
                  <TableCell align="right">Price</TableCell>
                  <TableCell align="right" sx={{ width: 120 }}>Return qty</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {sale.items.map((it) => (
                  <TableRow key={it.variantId}>
                    <TableCell>{it.sku} — {it.productName}</TableCell>
                    <TableCell align="right">{it.quantity}</TableCell>
                    <TableCell align="right">{formatMoney(it.unitPrice)}</TableCell>
                    <TableCell align="right">
                      <TextField
                        type="number" size="small"
                        value={qty[it.variantId] ?? 0}
                        onChange={(e) => {
                          const v = Math.max(0, Math.min(it.quantity, Number(e.target.value)));
                          setQty((q) => ({ ...q, [it.variantId]: v }));
                        }}
                        inputProps={{ min: 0, max: it.quantity }}
                        sx={{ width: 90 }}
                      />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
            <Stack direction="row" spacing={2} justifyContent="space-between" alignItems="center">
              <TextField select label="Refund method" value={refundMethod} onChange={(e) => setRefundMethod(e.target.value as PaymentMethod)} sx={{ width: 200 }}>
                {METHODS.map((m) => <MenuItem key={m} value={m}>{m}</MenuItem>)}
              </TextField>
              <TextField label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} sx={{ flexGrow: 1 }} />
            </Stack>
            <Alert severity={refundTotal > 0 ? "info" : "warning"}>
              Refund total: <strong>{formatMoney(refundTotal)}</strong>
              {refundTotal === 0 && " — set a return quantity on at least one line."}
            </Alert>
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" disabled={lines.length === 0 || createReturn.isPending} onClick={handleSubmit}>
          {createReturn.isPending ? "Processing…" : "Process return"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
