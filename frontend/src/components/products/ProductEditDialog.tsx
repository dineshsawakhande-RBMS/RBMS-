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
import Divider from "@mui/material/Divider";
import IconButton from "@mui/material/IconButton";
import CircularProgress from "@mui/material/CircularProgress";
import CloudUploadIcon from "@mui/icons-material/CloudUpload";
import CloseIcon from "@mui/icons-material/Close";
import {
  useProduct, useUpdateProduct, useProductImages, useUploadProductImage, useDeleteProductImage,
} from "@/features/products/hooks";
import { useToast } from "@/components/providers/ToastProvider";
import { mediaUrl } from "@/lib/config";

export default function ProductEditDialog({ productId, onClose }: { productId: string | null; onClose: () => void }) {
  const { data: product, isLoading } = useProduct(productId);
  const { data: images } = useProductImages(productId);
  const updateProduct = useUpdateProduct();
  const uploadImage = useUploadProductImage();
  const deleteImage = useDeleteProductImage();
  const toast = useToast();
  const [form, setForm] = useState({ name: "", gstRate: 0, hsnCode: "", isActive: true });
  const [error, setError] = useState<string | null>(null);

  const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file || !productId) return;
    try {
      await uploadImage.mutateAsync({ productId, file });
      toast("Media uploaded");
    } catch {
      toast("Upload failed — check file type/size (≤25 MB)", "error");
    }
  };

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

            <Divider>Images &amp; video</Divider>
            <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1.5 }}>
              {images?.map((m) => (
                <Box key={m.id} sx={{ position: "relative", width: 92, height: 92, borderRadius: 2, overflow: "hidden", border: (t) => `1px solid ${t.palette.divider}` }}>
                  {m.isVideo ? (
                    <video src={mediaUrl(m.url)} width={92} height={92} style={{ objectFit: "cover" }} muted />
                  ) : (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img src={mediaUrl(m.url)} alt="" width={92} height={92} style={{ objectFit: "cover" }} />
                  )}
                  <IconButton
                    size="small"
                    onClick={() => productId && deleteImage.mutate({ imageId: m.id, productId })}
                    sx={{ position: "absolute", top: 2, right: 2, bgcolor: "rgba(0,0,0,0.55)", color: "#fff", "&:hover": { bgcolor: "rgba(0,0,0,0.75)" } }}
                  >
                    <CloseIcon sx={{ fontSize: 16 }} />
                  </IconButton>
                </Box>
              ))}
              <Button
                component="label" variant="outlined"
                startIcon={uploadImage.isPending ? <CircularProgress size={16} /> : <CloudUploadIcon />}
                disabled={uploadImage.isPending}
                sx={{ width: 92, height: 92, flexDirection: "column", borderStyle: "dashed" }}
              >
                Upload
                <input hidden type="file" accept="image/jpeg,image/png,image/webp,image/gif,video/mp4,video/webm" onChange={handleUpload} />
              </Button>
            </Box>
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
