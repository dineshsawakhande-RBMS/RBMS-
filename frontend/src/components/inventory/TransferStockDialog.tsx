"use client";

import { useState } from "react";
import { AxiosError } from "axios";
import Dialog from "@mui/material/Dialog";
import DialogTitle from "@mui/material/DialogTitle";
import DialogContent from "@mui/material/DialogContent";
import DialogActions from "@mui/material/DialogActions";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import MenuItem from "@mui/material/MenuItem";
import Alert from "@mui/material/Alert";
import { useStores, useTransferStock } from "@/features/stores/hooks";
import { useStockLevels } from "@/features/inventory/hooks";
import { useToast } from "@/components/providers/ToastProvider";

export default function TransferStockDialog({
  open, fromStoreId, onClose,
}: { open: boolean; fromStoreId: string; onClose: () => void }) {
  const toast = useToast();
  const { data: stores } = useStores();
  const { data: stock } = useStockLevels(fromStoreId, { page: 1, pageSize: 200 });
  const transfer = useTransferStock();

  const [toStoreId, setToStoreId] = useState("");
  const [variantId, setVariantId] = useState("");
  const [quantity, setQuantity] = useState(1);
  const [notes, setNotes] = useState("");
  const [error, setError] = useState<string | null>(null);

  const destinations = (stores ?? []).filter((s) => s.isActive && s.id !== fromStoreId);
  const variants = stock?.items ?? [];
  const selected = variants.find((v) => v.variantId === variantId);
  const maxQty = selected?.quantityOnHand ?? 0;

  const reset = () => { setToStoreId(""); setVariantId(""); setQuantity(1); setNotes(""); setError(null); };
  const close = () => { reset(); onClose(); };

  const submit = async () => {
    setError(null);
    try {
      await transfer.mutateAsync({
        fromStoreId, toStoreId,
        lines: [{ variantId, quantity: Number(quantity) }],
        notes: notes || null,
      });
      toast("Stock transferred", "success");
      close();
    } catch (err) {
      const e = err as AxiosError<{ title?: string }>;
      setError(e.response?.status === 409 ? "Not enough stock at the source store." : (e.response?.data?.title ?? "Could not transfer stock."));
    }
  };

  const canSubmit = !!toStoreId && !!variantId && quantity > 0 && quantity <= maxQty && !transfer.isPending;

  return (
    <Dialog open={open} onClose={close} fullWidth maxWidth="sm">
      <DialogTitle>Transfer Stock</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}
          {destinations.length === 0 && (
            <Alert severity="info">Add another active store to transfer stock to.</Alert>
          )}
          <TextField select required label="Destination store" value={toStoreId} onChange={(e) => setToStoreId(e.target.value)}>
            {destinations.map((s) => <MenuItem key={s.id} value={s.id}>{s.name}</MenuItem>)}
          </TextField>
          <TextField select required label="Item" value={variantId} onChange={(e) => { setVariantId(e.target.value); setQuantity(1); }}>
            {variants.map((v) => (
              <MenuItem key={v.variantId} value={v.variantId}>
                {v.sku} — {v.productName} (on hand: {v.quantityOnHand})
              </MenuItem>
            ))}
          </TextField>
          <TextField
            label="Quantity" type="number" value={quantity}
            onChange={(e) => setQuantity(Math.max(0, Number(e.target.value)))}
            error={!!selected && quantity > maxQty}
            helperText={selected ? `Available at source: ${maxQty}` : " "}
            inputProps={{ min: 1, max: maxQty }}
          />
          <TextField label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={close}>Cancel</Button>
        <Button variant="contained" disabled={!canSubmit} onClick={submit}>
          {transfer.isPending ? "Transferring…" : "Transfer"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
