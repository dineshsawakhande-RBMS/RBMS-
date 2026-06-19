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
import { useProducts, useCreateProduct } from "@/features/products/hooks";

interface VariantForm {
  sku: string;
  size: string;
  color: string;
  purchasePrice: number;
  sellingPrice: number;
  reorderLevel: number;
}

const emptyVariant = (): VariantForm => ({ sku: "", size: "", color: "", purchasePrice: 0, sellingPrice: 0, reorderLevel: 5 });

export default function ProductsPage() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [name, setName] = useState("");
  const [gstRate, setGstRate] = useState(12);
  const [hsnCode, setHsnCode] = useState("");
  const [variants, setVariants] = useState<VariantForm[]>([emptyVariant()]);

  const { data, isFetching } = useProducts({ search: search || undefined, page: page + 1, pageSize });
  const createProduct = useCreateProduct();

  const updateVariant = (i: number, patch: Partial<VariantForm>) =>
    setVariants((vs) => vs.map((v, idx) => (idx === i ? { ...v, ...patch } : v)));

  const reset = () => {
    setName(""); setGstRate(12); setHsnCode(""); setVariants([emptyVariant()]); setError(null);
  };

  const handleCreate = async () => {
    setError(null);
    try {
      await createProduct.mutateAsync({
        name: name.trim(),
        hsnCode: hsnCode || null,
        gstRate: Number(gstRate),
        variants: variants.map((v) => ({
          sku: v.sku.trim(),
          size: v.size || null,
          color: v.color || null,
          purchasePrice: Number(v.purchasePrice),
          sellingPrice: Number(v.sellingPrice),
          reorderLevel: Number(v.reorderLevel),
        })),
      });
      setOpen(false);
      reset();
    } catch (err) {
      const e = err as AxiosError<{ title?: string; errors?: Record<string, string[]> }>;
      const firstErr = e.response?.data?.errors ? Object.values(e.response.data.errors)[0]?.[0] : undefined;
      setError(
        e.response?.status === 409
          ? "A variant SKU already exists."
          : (firstErr ?? e.response?.data?.title ?? "Could not create product."),
      );
    }
  };

  const canSubmit = !!name.trim() && variants.length > 0 && variants.every((v) => v.sku.trim()) && !createProduct.isPending;

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h1">Products</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setOpen(true)}>
          New Product
        </Button>
      </Stack>

      <Card elevation={0}>
        <Box sx={{ p: 2 }}>
          <TextField
            size="small"
            label="Search products"
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
                <TableCell>Brand</TableCell>
                <TableCell>Category</TableCell>
                <TableCell align="right">GST %</TableCell>
                <TableCell align="right">Variants</TableCell>
                <TableCell align="center">Status</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data?.items.map((p) => (
                <TableRow key={p.id} hover>
                  <TableCell>{p.name}</TableCell>
                  <TableCell>{p.brandName ?? "—"}</TableCell>
                  <TableCell>{p.categoryName ?? "—"}</TableCell>
                  <TableCell align="right">{p.gstRate}%</TableCell>
                  <TableCell align="right">{p.variantCount}</TableCell>
                  <TableCell align="center">
                    <Chip size="small" color={p.isActive ? "success" : "default"} label={p.isActive ? "Active" : "Inactive"} />
                  </TableCell>
                </TableRow>
              ))}
              {data && data.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} align="center" sx={{ py: 4, color: "text.secondary" }}>
                    No products found.
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
        <DialogTitle>New Product</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="Product name" required value={name} onChange={(e) => setName(e.target.value)} sx={{ flex: 2 }} />
              <TextField label="GST %" type="number" value={gstRate} onChange={(e) => setGstRate(Number(e.target.value))} sx={{ width: 110 }} />
              <TextField label="HSN code" value={hsnCode} onChange={(e) => setHsnCode(e.target.value)} sx={{ width: 140 }} />
            </Stack>

            <Divider>Variants</Divider>
            {variants.map((v, i) => (
              <Stack key={i} direction={{ xs: "column", tablet: "row" }} spacing={1} alignItems="center">
                <TextField label="SKU" required value={v.sku} onChange={(e) => updateVariant(i, { sku: e.target.value })} sx={{ flex: 1, minWidth: 140 }} />
                <TextField label="Size" value={v.size} onChange={(e) => updateVariant(i, { size: e.target.value })} sx={{ width: 90 }} />
                <TextField label="Color" value={v.color} onChange={(e) => updateVariant(i, { color: e.target.value })} sx={{ width: 110 }} />
                <TextField label="Cost" type="number" value={v.purchasePrice} onChange={(e) => updateVariant(i, { purchasePrice: Number(e.target.value) })} sx={{ width: 100 }} />
                <TextField label="Price" type="number" value={v.sellingPrice} onChange={(e) => updateVariant(i, { sellingPrice: Number(e.target.value) })} sx={{ width: 100 }} />
                <TextField label="Reorder" type="number" value={v.reorderLevel} onChange={(e) => updateVariant(i, { reorderLevel: Number(e.target.value) })} sx={{ width: 100 }} />
                <IconButton color="error" disabled={variants.length === 1} onClick={() => setVariants((vs) => vs.filter((_, idx) => idx !== i))}>
                  <DeleteIcon />
                </IconButton>
              </Stack>
            ))}
            <Button startIcon={<AddIcon />} onClick={() => setVariants((vs) => [...vs, emptyVariant()])} sx={{ alignSelf: "flex-start" }}>
              Add variant
            </Button>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button variant="contained" disabled={!canSubmit} onClick={handleCreate}>
            {createProduct.isPending ? "Saving…" : "Create product"}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
