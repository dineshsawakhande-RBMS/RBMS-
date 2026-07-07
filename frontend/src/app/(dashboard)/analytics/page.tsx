"use client";

import { useState } from "react";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Card from "@mui/material/Card";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import MenuItem from "@mui/material/MenuItem";
import Tabs from "@mui/material/Tabs";
import Tab from "@mui/material/Tab";
import Chip from "@mui/material/Chip";
import LinearProgress from "@mui/material/LinearProgress";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Alert from "@mui/material/Alert";
import { useTheme } from "@mui/material/styles";
import {
  ResponsiveContainer, BarChart, Bar, XAxis, YAxis, CartesianGrid, Legend, Tooltip as RechartsTooltip,
} from "recharts";
import { useDeadStock, useCustomerRetention } from "@/features/analytics/hooks";
import { DEFAULT_STORE_ID, formatMoney } from "@/lib/config";

const DAY_WINDOWS = [30, 60, 90, 180, 365];

export default function AnalyticsPage() {
  const [tab, setTab] = useState(0);

  return (
    <Box>
      <Typography variant="h1" sx={{ mb: 2 }}>Analytics</Typography>
      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 2 }}>
        <Tab label="Dead / Slow Stock" />
        <Tab label="Customer Retention" />
      </Tabs>
      {tab === 0 ? <DeadStockTab /> : <RetentionTab />}
    </Box>
  );
}

function DeadStockTab() {
  const [days, setDays] = useState(90);
  const [slowThreshold, setSlowThreshold] = useState(5);
  const { data, isFetching } = useDeadStock(DEFAULT_STORE_ID, days, slowThreshold);

  return (
    <>
      <Card elevation={0} sx={{ p: 2, mb: 2 }}>
        <Stack direction={{ xs: "column", tablet: "row" }} spacing={2} alignItems={{ tablet: "center" }}>
          <TextField select label="Window" value={days} onChange={(e) => setDays(Number(e.target.value))} sx={{ width: 160 }}>
            {DAY_WINDOWS.map((d) => <MenuItem key={d} value={d}>Last {d} days</MenuItem>)}
          </TextField>
          <TextField
            label="Slow threshold (units)" type="number" value={slowThreshold}
            onChange={(e) => setSlowThreshold(Math.max(0, Number(e.target.value)))} sx={{ width: 200 }}
            helperText="Sold ≤ this = slow; 0 sold = dead"
          />
        </Stack>
        {data && (
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap sx={{ mt: 2 }}>
            <Chip color="error" label={`Dead: ${data.deadCount} (${formatMoney(data.deadValue)})`} />
            <Chip color="warning" label={`Slow: ${data.slowCount} (${formatMoney(data.slowValue)})`} />
            <Chip color="primary" label={`Capital tied up: ${formatMoney(data.totalTiedValue)}`} />
          </Stack>
        )}
      </Card>

      <Card elevation={0}>
        {isFetching && <LinearProgress />}
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>SKU</TableCell>
                <TableCell>Product</TableCell>
                <TableCell align="right">On hand</TableCell>
                <TableCell align="right">Avg cost</TableCell>
                <TableCell align="right">Stock value</TableCell>
                <TableCell align="right">Sold (window)</TableCell>
                <TableCell align="right">Days since sale</TableCell>
                <TableCell align="center">Status</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data?.rows.map((r) => (
                <TableRow key={r.variantId} hover>
                  <TableCell>{r.sku}</TableCell>
                  <TableCell>{r.productName}</TableCell>
                  <TableCell align="right">{r.quantityOnHand}</TableCell>
                  <TableCell align="right">{formatMoney(r.avgCost)}</TableCell>
                  <TableCell align="right">{formatMoney(r.stockValue)}</TableCell>
                  <TableCell align="right">{r.unitsSold}</TableCell>
                  <TableCell align="right">{r.daysSinceLastSale ?? "never"}</TableCell>
                  <TableCell align="center">
                    <Chip size="small" color={r.isDead ? "error" : "warning"} label={r.isDead ? "Dead" : "Slow"} />
                  </TableCell>
                </TableRow>
              ))}
              {data && data.rows.length === 0 && (
                <TableRow>
                  <TableCell colSpan={8} align="center" sx={{ py: 4, color: "text.secondary" }}>
                    No slow or dead stock in this window — everything&apos;s moving. 🎉
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Card>
    </>
  );
}

function RetentionTab() {
  const theme = useTheme();
  const [months, setMonths] = useState(6);
  const { data, isFetching } = useCustomerRetention(months);

  return (
    <>
      <Card elevation={0} sx={{ p: 2, mb: 2 }}>
        <Stack direction={{ xs: "column", tablet: "row" }} spacing={2} alignItems={{ tablet: "center" }}>
          <TextField select label="Period" value={months} onChange={(e) => setMonths(Number(e.target.value))} sx={{ width: 160 }}>
            {[3, 6, 12, 24].map((m) => <MenuItem key={m} value={m}>Last {m} months</MenuItem>)}
          </TextField>
          {data && (
            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
              <Chip color="primary" label={`Customers: ${data.totalCustomers}`} />
              <Chip color="success" label={`Repeat rate: ${data.repeatRatePct}%`} />
              <Chip variant="outlined" label={`New (period): ${data.newCustomersInPeriod}`} />
              <Chip variant="outlined" label={`Avg orders: ${data.avgOrdersPerCustomer}`} />
              <Chip variant="outlined" label={`Avg spend: ${formatMoney(data.avgSpendPerCustomer)}`} />
            </Stack>
          )}
        </Stack>
      </Card>

      <Card elevation={0} sx={{ p: 2, mb: 2 }}>
        <Typography variant="h6" sx={{ mb: 1 }}>New vs returning customers</Typography>
        {isFetching && <LinearProgress sx={{ mb: 1 }} />}
        <Box sx={{ height: 300 }}>
          <ResponsiveContainer>
            <BarChart data={data?.trend ?? []}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} />
              <XAxis dataKey="label" />
              <YAxis allowDecimals={false} />
              <RechartsTooltip />
              <Legend />
              <Bar dataKey="newCustomers" name="New" stackId="a" fill={theme.palette.primary.main} />
              <Bar dataKey="returningCustomers" name="Returning" stackId="a" fill={theme.palette.success.main} />
            </BarChart>
          </ResponsiveContainer>
        </Box>
      </Card>

      <Card elevation={0}>
        <Typography variant="h6" sx={{ p: 2, pb: 0 }}>Top customers by spend</Typography>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Customer</TableCell>
                <TableCell>Mobile</TableCell>
                <TableCell align="right">Orders</TableCell>
                <TableCell align="right">Total spend</TableCell>
                <TableCell>Last purchase</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data?.topCustomers.map((c) => (
                <TableRow key={c.customerId} hover>
                  <TableCell>{c.name}</TableCell>
                  <TableCell>{c.mobile}</TableCell>
                  <TableCell align="right">{c.orders}</TableCell>
                  <TableCell align="right"><strong>{formatMoney(c.totalSpend)}</strong></TableCell>
                  <TableCell>{c.lastPurchase.slice(0, 10)}</TableCell>
                </TableRow>
              ))}
              {data && data.topCustomers.length === 0 && (
                <TableRow>
                  <TableCell colSpan={5} align="center" sx={{ py: 4, color: "text.secondary" }}>
                    No customer sales yet.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Card>
    </>
  );
}
