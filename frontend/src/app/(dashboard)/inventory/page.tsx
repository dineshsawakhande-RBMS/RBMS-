"use client";

import { useState } from "react";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Card from "@mui/material/Card";
import TextField from "@mui/material/TextField";
import FormControlLabel from "@mui/material/FormControlLabel";
import Switch from "@mui/material/Switch";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import TablePagination from "@mui/material/TablePagination";
import Chip from "@mui/material/Chip";
import LinearProgress from "@mui/material/LinearProgress";
import { useStockLevels } from "@/features/inventory/hooks";
import { formatMoney, formatNumber } from "@/lib/config";

export default function InventoryPage() {
  const [search, setSearch] = useState("");
  const [lowStockOnly, setLowStockOnly] = useState(false);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);

  const { data, isFetching } = useStockLevels({
    search: search || undefined,
    lowStockOnly,
    page: page + 1,
    pageSize,
  });

  return (
    <Box>
      <Typography variant="h1" gutterBottom>
        Inventory
      </Typography>

      <Card elevation={2}>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ p: 2, alignItems: "center" }}>
          <TextField
            size="small"
            label="Search SKU or product"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(0);
            }}
            sx={{ width: { xs: "100%", sm: 320 } }}
          />
          <FormControlLabel
            control={<Switch checked={lowStockOnly} onChange={(e) => { setLowStockOnly(e.target.checked); setPage(0); }} />}
            label="Low stock only"
          />
        </Stack>
        {isFetching && <LinearProgress />}
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>SKU</TableCell>
                <TableCell>Product</TableCell>
                <TableCell>Variant</TableCell>
                <TableCell align="right">On Hand</TableCell>
                <TableCell align="right">Reorder</TableCell>
                <TableCell align="right">Avg Cost</TableCell>
                <TableCell align="right">Stock Value</TableCell>
                <TableCell align="center">Status</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data?.items.map((s) => (
                <TableRow key={s.variantId} hover>
                  <TableCell>{s.sku}</TableCell>
                  <TableCell>{s.productName}</TableCell>
                  <TableCell>{[s.size, s.color].filter(Boolean).join(" / ") || "—"}</TableCell>
                  <TableCell align="right">{formatNumber(s.quantityOnHand)}</TableCell>
                  <TableCell align="right">{formatNumber(s.reorderLevel)}</TableCell>
                  <TableCell align="right">{formatMoney(s.avgCost)}</TableCell>
                  <TableCell align="right">{formatMoney(s.stockValue)}</TableCell>
                  <TableCell align="center">
                    <Chip size="small" color={s.isLow ? "warning" : "success"} label={s.isLow ? "Low" : "OK"} />
                  </TableCell>
                </TableRow>
              ))}
              {data && data.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={8} align="center" sx={{ py: 4, color: "text.secondary" }}>
                    No stock records.
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
          onRowsPerPageChange={(e) => {
            setPageSize(parseInt(e.target.value, 10));
            setPage(0);
          }}
          rowsPerPageOptions={[10, 20, 50]}
        />
      </Card>
    </Box>
  );
}
