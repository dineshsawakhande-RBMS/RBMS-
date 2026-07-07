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
import Dialog from "@mui/material/Dialog";
import DialogTitle from "@mui/material/DialogTitle";
import DialogContent from "@mui/material/DialogContent";
import DialogActions from "@mui/material/DialogActions";
import Alert from "@mui/material/Alert";
import Autocomplete from "@mui/material/Autocomplete";
import WhatsAppIcon from "@mui/icons-material/WhatsApp";
import type { WhatsAppMessageKind, WhatsAppMessageStatus } from "@/types";
import { useWhatsAppMessages, useSendWhatsApp } from "@/features/whatsapp/hooks";
import { useCustomers } from "@/features/customers/hooks";
import { useToast } from "@/components/providers/ToastProvider";

const KINDS: WhatsAppMessageKind[] = ["Custom", "Promotion", "PaymentReminder", "Invoice", "LowStockAlert"];
const statusColor = (s: WhatsAppMessageStatus) => (s === "Sent" ? "success" : s === "Failed" ? "error" : "warning");

export default function WhatsAppPage() {
  const toast = useToast();
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState({ toPhone: "", recipientName: "", kind: "Custom" as WhatsAppMessageKind, body: "" });

  const { data, isFetching } = useWhatsAppMessages({ page: page + 1, pageSize });
  const { data: customers } = useCustomers({ page: 1, pageSize: 100 });
  const send = useSendWhatsApp();

  const submit = async () => {
    setError(null);
    try {
      await send.mutateAsync({
        toPhone: form.toPhone.trim(),
        recipientName: form.recipientName || null,
        kind: form.kind,
        body: form.body.trim(),
      });
      toast("Message sent", "success");
      setOpen(false);
      setForm({ toPhone: "", recipientName: "", kind: "Custom", body: "" });
    } catch (err) {
      setError((err as AxiosError<{ title?: string }>).response?.data?.title ?? "Could not send message.");
    }
  };

  const canSubmit = !!form.toPhone.trim() && !!form.body.trim() && !send.isPending;

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h1">WhatsApp</Typography>
        <Button variant="contained" color="success" startIcon={<WhatsAppIcon />} onClick={() => setOpen(true)}>
          Send message
        </Button>
      </Stack>

      <Alert severity="info" sx={{ mb: 2 }}>
        Messages are delivered through a local stub (recorded here, no external send) until a WhatsApp provider is connected.
      </Alert>

      <Card elevation={0}>
        {isFetching && <LinearProgress />}
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>To</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>Message</TableCell>
                <TableCell align="center">Status</TableCell>
                <TableCell>Sent</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data?.items.map((m) => (
                <TableRow key={m.id} hover>
                  <TableCell>{m.recipientName ? `${m.recipientName} (${m.toPhone})` : m.toPhone}</TableCell>
                  <TableCell>{m.kind}</TableCell>
                  <TableCell sx={{ maxWidth: 420, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{m.body}</TableCell>
                  <TableCell align="center"><Chip size="small" color={statusColor(m.status)} label={m.status} /></TableCell>
                  <TableCell>{m.sentAt ? new Date(m.sentAt).toLocaleString("en-IN") : "—"}</TableCell>
                </TableRow>
              ))}
              {data && data.items.length === 0 && (
                <TableRow><TableCell colSpan={5} align="center" sx={{ py: 4, color: "text.secondary" }}>No messages sent yet.</TableCell></TableRow>
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
        <DialogTitle>Send WhatsApp Message</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <Autocomplete
              options={customers?.items ?? []}
              getOptionLabel={(o) => `${o.name} (${o.mobile})`}
              onChange={(_, v) => setForm((f) => ({ ...f, toPhone: v?.mobile ?? f.toPhone, recipientName: v?.name ?? f.recipientName }))}
              renderInput={(params) => <TextField {...params} label="Pick a customer (optional)" />}
              isOptionEqualToValue={(o, v) => o.id === v.id}
            />
            <Stack direction={{ xs: "column", tablet: "row" }} spacing={2}>
              <TextField label="Phone" required value={form.toPhone} onChange={(e) => setForm((f) => ({ ...f, toPhone: e.target.value }))} sx={{ flex: 1 }} />
              <TextField label="Recipient name" value={form.recipientName} onChange={(e) => setForm((f) => ({ ...f, recipientName: e.target.value }))} sx={{ flex: 1 }} />
              <TextField select label="Type" value={form.kind} onChange={(e) => setForm((f) => ({ ...f, kind: e.target.value as WhatsAppMessageKind }))} sx={{ flex: 1 }}>
                {KINDS.map((k) => <MenuItem key={k} value={k}>{k}</MenuItem>)}
              </TextField>
            </Stack>
            <TextField label="Message" required value={form.body} onChange={(e) => setForm((f) => ({ ...f, body: e.target.value }))} multiline minRows={3} inputProps={{ maxLength: 2000 }} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button variant="contained" color="success" disabled={!canSubmit} onClick={submit}>
            {send.isPending ? "Sending…" : "Send"}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
