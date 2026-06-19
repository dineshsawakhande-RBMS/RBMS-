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
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import type { DocumentType } from "@/types";
import { useDocument, useUpdateDocument } from "@/features/documents/hooks";
import { DOCUMENT_TYPES } from "@/features/documents/constants";
import { useToast } from "@/components/providers/ToastProvider";

export default function DocumentEditDialog({ documentId, onClose }: { documentId: string | null; onClose: () => void }) {
  const { data, isLoading } = useDocument(documentId);
  const update = useUpdateDocument();
  const toast = useToast();
  const [form, setForm] = useState({
    title: "", documentType: "Other" as DocumentType, description: "", tags: "", issueDate: "", expiryDate: "",
  });
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (data) {
      setForm({
        title: data.title,
        documentType: data.documentType,
        description: data.description ?? "",
        tags: data.tags.join(", "),
        issueDate: data.issueDate ?? "",
        expiryDate: data.expiryDate ?? "",
      });
      setError(null);
    }
  }, [data]);

  const set = (k: keyof typeof form, v: string) => setForm((f) => ({ ...f, [k]: v }));

  const save = async () => {
    if (!data) return;
    setError(null);
    try {
      await update.mutateAsync({
        id: data.id,
        title: form.title.trim(),
        documentType: form.documentType,
        description: form.description || null,
        tags: form.tags || null,
        issueDate: form.issueDate || null,
        expiryDate: form.expiryDate || null,
      });
      toast("Document updated");
      onClose();
    } catch (err) {
      setError((err as AxiosError<{ title?: string }>).response?.data?.title ?? "Could not update document.");
    }
  };

  return (
    <Dialog open={!!documentId} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Edit Document</DialogTitle>
      <DialogContent>
        {isLoading || !data ? (
          <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}><CircularProgress /></Box>
        ) : (
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField label="File" value={data.fileName} disabled helperText="Re-upload as a new document to replace the file." />
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="Title" required value={form.title} onChange={(e) => set("title", e.target.value)} sx={{ flex: 2 }} />
              <TextField select label="Type" value={form.documentType} onChange={(e) => set("documentType", e.target.value)} sx={{ flex: 1 }}>
                {DOCUMENT_TYPES.map((t) => <MenuItem key={t.value} value={t.value}>{t.label}</MenuItem>)}
              </TextField>
            </Stack>
            <TextField label="Description" value={form.description} onChange={(e) => set("description", e.target.value)} multiline minRows={2} />
            <TextField label="Tags (comma-separated)" value={form.tags} onChange={(e) => set("tags", e.target.value)} />
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="Issue date" type="date" value={form.issueDate} onChange={(e) => set("issueDate", e.target.value)} InputLabelProps={{ shrink: true }} sx={{ flex: 1 }} />
              <TextField label="Expiry date" type="date" value={form.expiryDate} onChange={(e) => set("expiryDate", e.target.value)} InputLabelProps={{ shrink: true }} sx={{ flex: 1 }} />
            </Stack>
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" disabled={!form.title || update.isPending} onClick={save}>
          {update.isPending ? "Saving…" : "Save changes"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
