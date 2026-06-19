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
import Typography from "@mui/material/Typography";
import CircularProgress from "@mui/material/CircularProgress";
import { useProduct, useUpdateProduct } from "@/features/products/hooks";

export default function ProductEditDialog({ productId, onClose }: { productId: string | null; onClose: () => void }) {
  const { data: product, isLoading } = useProduct(productId);
  const updateProduct = useUpdateProduct();
  const [form, setForm] = useState({ name: "", gstRate: 0, hsnCode: "", isActive: true });
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (product) {
      setForm({ name: product.name, gstRate: product.gstRate, hsnCode: product.hsnCode ?? "", isActive: product.isActive });
      setError(null);
    }
  }, [product]);

  const handleSave = async () => {
    if (!product) return;
    setError(null);
    try {
      await updateProduct.mutateAsync({
        id: product.id,
        name: form.name.trim(),
        hsnCode: form.hsnCode || null,
        gstRate: Number(form.gstRate),
        isActive: form.isActive,
      });
      onClose();
    } catch (err) {
      const e = err as AxiosError<{ title?: string }>;
      setError(e.response?.data?.title ?? "Could not update the product.");
    }
  };

  return (
    <Dialog open={!!productId} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Edit Product</DialogTitle>
      <DialogContent>
        {isLoading || !product ? (
          <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}><CircularProgress /></Box>
        ) : (
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField label="Name" required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
            <Stack direction="row" spacing={2}>
              <TextField label="GST %" type="number" value={form.gstRate} onChange={(e) => setForm({ ...form, gstRate: Number(e.target.value) })} sx={{ width: 120 }} />
              <TextField label="HSN code" value={form.hsnCode} onChange={(e) => setForm({ ...form, hsnCode: e.target.value })} sx={{ flexGrow: 1 }} />
            </Stack>
            <FormControlLabel
              control={<Switch checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} />}
              label="Active"
            />
            <Typography variant="caption" color="text.secondary">
              {product.variants.length} variant(s). Variant prices/stock are managed via Inventory & Purchases.
            </Typography>
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" disabled={!form.name || updateProduct.isPending} onClick={handleSave}>
          {updateProduct.isPending ? "Saving…" : "Save changes"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
