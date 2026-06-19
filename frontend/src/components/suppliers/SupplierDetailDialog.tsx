"use client";

import Dialog from "@mui/material/Dialog";
import DialogTitle from "@mui/material/DialogTitle";
import DialogContent from "@mui/material/DialogContent";
import DialogActions from "@mui/material/DialogActions";
import Button from "@mui/material/Button";
import Box from "@mui/material/Box";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import Chip from "@mui/material/Chip";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import CircularProgress from "@mui/material/CircularProgress";
import { useSupplierLedger } from "@/features/suppliers/hooks";
import { formatMoney } from "@/lib/config";

export default function SupplierDetailDialog({ supplierId, onClose }: { supplierId: string | null; onClose: () => void }) {
  const { data, isLoading } = useSupplierLedger(supplierId);

  return (
    <Dialog open={!!supplierId} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>{data?.name ?? "Supplier"} — Ledger</DialogTitle>
      <DialogContent>
        {isLoading || !data ? (
          <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}><CircularProgress /></Box>
        ) : (
          <Stack spacing={2} sx={{ mt: 1 }}>
            <Stack direction="row" spacing={2} alignItems="center">
              <Typography variant="body1">Outstanding balance:</Typography>
              <Chip
                color={data.outstanding > 0 ? "warning" : "success"}
                label={formatMoney(data.outstanding)}
                sx={{ fontSize: 16, fontWeight: 700, height: 32 }}
              />
              <Typography variant="caption" color="text.secondary">
                {data.outstanding > 0 ? "(we owe the supplier)" : "(settled)"}
              </Typography>
            </Stack>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Date</TableCell>
                  <TableCell>Type</TableCell>
                  <TableCell align="right">Debit</TableCell>
                  <TableCell align="right">Credit</TableCell>
                  <TableCell align="right">Balance</TableCell>
                  <TableCell>Notes</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data.entries.map((e, i) => (
                  <TableRow key={i} hover>
                    <TableCell>{new Date(e.entryDate).toLocaleDateString("en-IN")}</TableCell>
                    <TableCell>{e.referenceType}</TableCell>
                    <TableCell align="right">{e.debit ? formatMoney(e.debit) : "—"}</TableCell>
                    <TableCell align="right">{e.credit ? formatMoney(e.credit) : "—"}</TableCell>
                    <TableCell align="right">{formatMoney(e.runningBalance)}</TableCell>
                    <TableCell>{e.notes ?? "—"}</TableCell>
                  </TableRow>
                ))}
                {data.entries.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={6} align="center" sx={{ py: 3, color: "text.secondary" }}>
                      No ledger entries yet.
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  );
}
