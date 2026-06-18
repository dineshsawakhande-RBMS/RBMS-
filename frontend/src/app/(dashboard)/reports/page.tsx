"use client";

import { useState } from "react";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Card from "@mui/material/Card";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import MenuItem from "@mui/material/MenuItem";
import Button from "@mui/material/Button";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import LinearProgress from "@mui/material/LinearProgress";
import Chip from "@mui/material/Chip";
import DownloadIcon from "@mui/icons-material/Download";
import { useReport, downloadReportCsv, type ReportType, type ReportParams } from "@/features/reports/hooks";
import { DEFAULT_STORE_ID, formatMoney, formatNumber } from "@/lib/config";

interface Column {
  key: string;
  label: string;
  align?: "left" | "right";
  fmt?: "money" | "num" | "date" | "datetime";
}

const CONFIG: Record<ReportType, { label: string; dated: boolean; columns: Column[] }> = {
  sales: {
    label: "Sales",
    dated: true,
    columns: [
      { key: "invoiceNumber", label: "Invoice" },
      { key: "date", label: "Date", fmt: "datetime" },
      { key: "taxable", label: "Taxable", align: "right", fmt: "money" },
      { key: "tax", label: "Tax", align: "right", fmt: "money" },
      { key: "grandTotal", label: "Total", align: "right", fmt: "money" },
      { key: "paymentStatus", label: "Payment" },
    ],
  },
  purchases: {
    label: "Purchases",
    dated: true,
    columns: [
      { key: "invoiceNumber", label: "Invoice" },
      { key: "supplierName", label: "Supplier" },
      { key: "date", label: "Date", fmt: "date" },
      { key: "grandTotal", label: "Total", align: "right", fmt: "money" },
      { key: "amountPaid", label: "Paid", align: "right", fmt: "money" },
      { key: "paymentStatus", label: "Payment" },
    ],
  },
  inventory: {
    label: "Inventory Valuation",
    dated: false,
    columns: [
      { key: "sku", label: "SKU" },
      { key: "productName", label: "Product" },
      { key: "quantityOnHand", label: "On Hand", align: "right", fmt: "num" },
      { key: "avgCost", label: "Avg Cost", align: "right", fmt: "money" },
      { key: "stockValue", label: "Stock Value", align: "right", fmt: "money" },
    ],
  },
  profit: {
    label: "Profit by Product",
    dated: true,
    columns: [
      { key: "productName", label: "Product" },
      { key: "quantitySold", label: "Qty Sold", align: "right", fmt: "num" },
      { key: "revenue", label: "Revenue", align: "right", fmt: "money" },
      { key: "cogs", label: "COGS", align: "right", fmt: "money" },
      { key: "profit", label: "Profit", align: "right", fmt: "money" },
    ],
  },
};

const today = () => new Date().toISOString().slice(0, 10);
const monthStart = () => { const d = new Date(); return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10); };

function render(value: unknown, fmt?: Column["fmt"]): string {
  if (value === null || value === undefined) return "—";
  switch (fmt) {
    case "money": return formatMoney(Number(value));
    case "num": return formatNumber(Number(value));
    case "date": return new Date(String(value)).toLocaleDateString("en-IN");
    case "datetime": return new Date(String(value)).toLocaleString("en-IN");
    default: return String(value);
  }
}

export default function ReportsPage() {
  const [type, setType] = useState<ReportType>("sales");
  const [from, setFrom] = useState(monthStart());
  const [to, setTo] = useState(today());
  const [downloading, setDownloading] = useState(false);

  const cfg = CONFIG[type];
  const params: ReportParams = cfg.dated
    ? { from, to }
    : { storeId: DEFAULT_STORE_ID };

  const { data, isFetching } = useReport(type, params);

  const handleDownload = async () => {
    setDownloading(true);
    try {
      await downloadReportCsv(type, params);
    } finally {
      setDownloading(false);
    }
  };

  return (
    <Box>
      <Typography variant="h1" gutterBottom>
        Reports
      </Typography>

      <Card elevation={2} sx={{ p: 2, mb: 2 }}>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} alignItems={{ sm: "center" }}>
          <TextField select label="Report" value={type} onChange={(e) => setType(e.target.value as ReportType)} sx={{ minWidth: 200 }}>
            {(Object.keys(CONFIG) as ReportType[]).map((t) => (
              <MenuItem key={t} value={t}>{CONFIG[t].label}</MenuItem>
            ))}
          </TextField>
          {cfg.dated && (
            <>
              <TextField label="From" type="date" value={from} onChange={(e) => setFrom(e.target.value)} InputLabelProps={{ shrink: true }} />
              <TextField label="To" type="date" value={to} onChange={(e) => setTo(e.target.value)} InputLabelProps={{ shrink: true }} />
            </>
          )}
          <Box sx={{ flexGrow: 1 }} />
          <Button variant="outlined" startIcon={<DownloadIcon />} onClick={handleDownload} disabled={downloading}>
            {downloading ? "Preparing…" : "Download CSV"}
          </Button>
        </Stack>
      </Card>

      <Card elevation={2}>
        {isFetching && <LinearProgress />}
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                {cfg.columns.map((c) => (
                  <TableCell key={c.key} align={c.align ?? "left"}>{c.label}</TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody>
              {data?.rows?.map((row, i) => (
                <TableRow key={i} hover>
                  {cfg.columns.map((c) => (
                    <TableCell key={c.key} align={c.align ?? "left"}>
                      {c.key === "paymentStatus"
                        ? <Chip size="small" label={String(row[c.key] ?? "")} color={row[c.key] === "Paid" ? "success" : "default"} />
                        : render(row[c.key], c.fmt)}
                    </TableCell>
                  ))}
                </TableRow>
              ))}
              {data && (!data.rows || data.rows.length === 0) && (
                <TableRow>
                  <TableCell colSpan={cfg.columns.length} align="center" sx={{ py: 4, color: "text.secondary" }}>
                    No data for the selected range.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Card>
    </Box>
  );
}
