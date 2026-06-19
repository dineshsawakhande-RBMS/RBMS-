"use client";

import { useState } from "react";
import { AxiosError } from "axios";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Card from "@mui/material/Card";
import Button from "@mui/material/Button";
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
import IconButton from "@mui/material/IconButton";
import Tooltip from "@mui/material/Tooltip";
import Dialog from "@mui/material/Dialog";
import DialogTitle from "@mui/material/DialogTitle";
import DialogContent from "@mui/material/DialogContent";
import DialogActions from "@mui/material/DialogActions";
import Alert from "@mui/material/Alert";
import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import DownloadIcon from "@mui/icons-material/Download";
import UploadFileIcon from "@mui/icons-material/UploadFile";
import type { DocumentType } from "@/types";
import { useDocuments, useUploadDocument, useDeleteDocument, useExpiringDocuments } from "@/features/documents/hooks";
import { DOCUMENT_TYPES, documentTypeLabel, formatFileSize } from "@/features/documents/constants";
import { useToast } from "@/components/providers/ToastProvider";
import ConfirmDialog from "@/components/common/ConfirmDialog";
import DocumentEditDialog from "@/components/documents/DocumentEditDialog";
import { mediaUrl } from "@/lib/config";

const EXPIRY_WINDOW_DAYS = 30;
const isExpiringSoon = (expiry: string | null) => {
  if (!expiry) return false;
  const days = (new Date(expiry).getTime() - Date.now()) / (1000 * 60 * 60 * 24);
  return days <= EXPIRY_WINDOW_DAYS;
};

const emptyForm = {
  title: "",
  documentType: "GstCertificate" as DocumentType,
  description: "",
  tags: "",
  issueDate: "",
  expiryDate: "",
};

export default function DocumentsPage() {
  const toast = useToast();
  const [search, setSearch] = useState("");
  const [typeFilter, setTypeFilter] = useState<DocumentType | "">("");
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);

  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [file, setFile] = useState<File | null>(null);

  const [deleteTarget, setDeleteTarget] = useState<{ id: string; title: string } | null>(null);
  const [editId, setEditId] = useState<string | null>(null);

  const { data, isFetching } = useDocuments({
    search: search || undefined,
    documentType: typeFilter || undefined,
    page: page + 1,
    pageSize,
  });
  const { data: expiring } = useExpiringDocuments(EXPIRY_WINDOW_DAYS);
  const upload = useUploadDocument();
  const remove = useDeleteDocument();

  const set = (k: keyof typeof form, v: string) => setForm((f) => ({ ...f, [k]: v }));

  const resetForm = () => { setForm(emptyForm); setFile(null); setError(null); };

  const handleUpload = async () => {
    if (!file) return;
    setError(null);
    try {
      await upload.mutateAsync({
        file,
        title: form.title.trim(),
        documentType: form.documentType,
        description: form.description || undefined,
        tags: form.tags || undefined,
        issueDate: form.issueDate || undefined,
        expiryDate: form.expiryDate || undefined,
      });
      toast(`${form.title} uploaded`, "success");
      setOpen(false);
      resetForm();
    } catch (err) {
      const e = err as AxiosError<{ title?: string }>;
      setError(e.response?.data?.title ?? "Could not upload the document.");
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    try {
      await remove.mutateAsync(deleteTarget.id);
      toast(`${deleteTarget.title} deleted`, "info");
    } catch {
      toast("Could not delete document", "error");
    } finally {
      setDeleteTarget(null);
    }
  };

  const canSubmit = !!file && !!form.title.trim() && !upload.isPending;

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h1">Documents</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setOpen(true)}>
          Upload
        </Button>
      </Stack>

      {expiring && expiring.length > 0 && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          {expiring.length} document{expiring.length > 1 ? "s" : ""} expiring within {EXPIRY_WINDOW_DAYS} days
          {": "}
          {expiring.slice(0, 3).map((d) => d.title).join(", ")}
          {expiring.length > 3 ? "…" : ""}
        </Alert>
      )}

      <Card elevation={0}>
        <Stack direction={{ xs: "column", tablet: "row" }} spacing={2} sx={{ p: 2 }}>
          <TextField
            size="small" label="Search title, file or tags" value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(0); }}
            sx={{ width: { xs: "100%", tablet: 340 } }}
          />
          <TextField
            select size="small" label="Type" value={typeFilter}
            onChange={(e) => { setTypeFilter(e.target.value as DocumentType | ""); setPage(0); }}
            sx={{ width: { xs: "100%", tablet: 220 } }}
          >
            <MenuItem value="">All types</MenuItem>
            {DOCUMENT_TYPES.map((t) => <MenuItem key={t.value} value={t.value}>{t.label}</MenuItem>)}
          </TextField>
        </Stack>
        {isFetching && <LinearProgress />}
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Title</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>File</TableCell>
                <TableCell>Tags</TableCell>
                <TableCell>Expiry</TableCell>
                <TableCell align="right">Size</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data?.items.map((d) => (
                <TableRow key={d.id} hover>
                  <TableCell>{d.title}</TableCell>
                  <TableCell>{documentTypeLabel(d.documentType)}</TableCell>
                  <TableCell sx={{ maxWidth: 200, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                    {d.fileName}
                  </TableCell>
                  <TableCell>
                    <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap>
                      {d.tags.map((t) => <Chip key={t} size="small" label={t} variant="outlined" />)}
                    </Stack>
                  </TableCell>
                  <TableCell>
                    {d.expiryDate
                      ? <Chip size="small" label={d.expiryDate} color={isExpiringSoon(d.expiryDate) ? "warning" : "default"} />
                      : "—"}
                  </TableCell>
                  <TableCell align="right">{formatFileSize(d.fileSizeBytes)}</TableCell>
                  <TableCell align="right">
                    <Tooltip title="Download">
                      <IconButton size="small" component="a" href={mediaUrl(d.downloadUrl)} target="_blank" rel="noopener">
                        <DownloadIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Edit">
                      <IconButton size="small" onClick={() => setEditId(d.id)}>
                        <EditIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Delete">
                      <IconButton size="small" color="error" onClick={() => setDeleteTarget({ id: d.id, title: d.title })}>
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
              {data && data.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} align="center" sx={{ py: 4, color: "text.secondary" }}>
                    No documents yet.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
        <TablePagination
          component="div" count={data?.totalCount ?? 0} page={page}
          onPageChange={(_, p) => setPage(p)} rowsPerPage={pageSize}
          onRowsPerPageChange={(e) => { setPageSize(parseInt(e.target.value, 10)); setPage(0); }}
          rowsPerPageOptions={[10, 20, 50]}
        />
      </Card>

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>Upload Document</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <Button
              variant="outlined" component="label" startIcon={<UploadFileIcon />}
              color={file ? "success" : "primary"}
            >
              {file ? file.name : "Choose file (PDF, image, Word, Excel — max 25 MB)"}
              <input
                type="file" hidden
                accept=".pdf,.jpg,.jpeg,.png,.webp,.gif,.doc,.docx,.xls,.xlsx,.txt,.csv"
                onChange={(e) => setFile(e.target.files?.[0] ?? null)}
              />
            </Button>
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="Title" required value={form.title} onChange={(e) => set("title", e.target.value)} sx={{ flex: 2 }} />
              <TextField select label="Type" value={form.documentType} onChange={(e) => set("documentType", e.target.value)} sx={{ flex: 1 }}>
                {DOCUMENT_TYPES.map((t) => <MenuItem key={t.value} value={t.value}>{t.label}</MenuItem>)}
              </TextField>
            </Stack>
            <TextField label="Description" value={form.description} onChange={(e) => set("description", e.target.value)} multiline minRows={2} />
            <TextField label="Tags (comma-separated)" value={form.tags} onChange={(e) => set("tags", e.target.value)} placeholder="gst, legal, 2026" />
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="Issue date" type="date" value={form.issueDate} onChange={(e) => set("issueDate", e.target.value)} InputLabelProps={{ shrink: true }} sx={{ flex: 1 }} />
              <TextField label="Expiry date" type="date" value={form.expiryDate} onChange={(e) => set("expiryDate", e.target.value)} InputLabelProps={{ shrink: true }} sx={{ flex: 1 }} />
            </Stack>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => { setOpen(false); resetForm(); }}>Cancel</Button>
          <Button variant="contained" disabled={!canSubmit} onClick={handleUpload}>
            {upload.isPending ? "Uploading…" : "Upload"}
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={!!deleteTarget}
        title="Delete document"
        message={`Delete "${deleteTarget?.title ?? "this document"}"? It'll be removed from lists but kept for audit.`}
        loading={remove.isPending}
        onConfirm={handleDelete}
        onClose={() => setDeleteTarget(null)}
      />

      <DocumentEditDialog documentId={editId} onClose={() => setEditId(null)} />
    </Box>
  );
}
