"use client";

import Grid from "@mui/material/Grid2";
import Box from "@mui/material/Box";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import CardHeader from "@mui/material/CardHeader";
import Typography from "@mui/material/Typography";
import Skeleton from "@mui/material/Skeleton";
import Chip from "@mui/material/Chip";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import { useTheme } from "@mui/material/styles";
import PointOfSaleIcon from "@mui/icons-material/PointOfSale";
import TrendingUpIcon from "@mui/icons-material/TrendingUp";
import SavingsIcon from "@mui/icons-material/Savings";
import Inventory2Icon from "@mui/icons-material/Inventory2";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import ShoppingCartIcon from "@mui/icons-material/ShoppingCart";
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  BarChart,
  Bar,
} from "recharts";
import { StatCard } from "@/components/dashboard/StatCard";
import { useDashboardSummary } from "@/features/dashboard/hooks";

function formatCurrency(value: number, currency: string): string {
  return new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency,
    maximumFractionDigits: 0,
  }).format(value);
}

export default function DashboardPage() {
  const theme = useTheme();
  const { data, isLoading } = useDashboardSummary();

  if (isLoading || !data) {
    return (
      <Grid container spacing={3}>
        {Array.from({ length: 6 }).map((_, i) => (
          <Grid key={i} size={{ xs: 12, sm: 6, md: 4 }}>
            <Skeleton variant="rounded" height={120} />
          </Grid>
        ))}
        <Grid size={{ xs: 12, md: 8 }}>
          <Skeleton variant="rounded" height={320} />
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <Skeleton variant="rounded" height={320} />
        </Grid>
      </Grid>
    );
  }

  const c = data.currency;

  const stats = [
    {
      title: "Today's Sales",
      value: formatCurrency(data.todaySales, c),
      icon: <PointOfSaleIcon />,
      color: theme.palette.primary.main,
    },
    {
      title: "Monthly Sales",
      value: formatCurrency(data.monthlySales, c),
      icon: <TrendingUpIcon />,
      color: theme.palette.secondary.main,
    },
    {
      title: "Monthly Profit",
      value: formatCurrency(data.monthlyProfit, c),
      icon: <SavingsIcon />,
      color: theme.palette.success.main,
    },
    {
      title: "Inventory Value",
      value: formatCurrency(data.inventoryValue, c),
      icon: <Inventory2Icon />,
      color: theme.palette.info.main,
    },
    {
      title: "Low Stock Items",
      value: String(data.lowStockCount),
      icon: <WarningAmberIcon />,
      color: theme.palette.warning.main,
      subtitle: "below reorder level",
    },
    {
      title: "Pending Orders",
      value: String(data.pendingOrders),
      icon: <ShoppingCartIcon />,
      color: theme.palette.error.main,
    },
  ];

  return (
    <Box>
      <Typography variant="h1" gutterBottom>
        Dashboard
      </Typography>

      <Grid container spacing={3}>
        {stats.map((s) => (
          <Grid key={s.title} size={{ xs: 12, sm: 6, md: 4 }}>
            <StatCard {...s} />
          </Grid>
        ))}

        <Grid size={{ xs: 12, md: 8 }}>
          <Card elevation={2}>
            <CardHeader title="Sales Trend" subheader="Last 6 months" />
            <CardContent>
              <Box sx={{ width: "100%", height: 300 }}>
                <ResponsiveContainer>
                  <LineChart data={data.salesTrend}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="label" />
                    <YAxis
                      tickFormatter={(v: number) =>
                        new Intl.NumberFormat("en-IN", { notation: "compact" }).format(v)
                      }
                    />
                    <RechartsTooltip
                      formatter={(value: number) => formatCurrency(value, c)}
                    />
                    <Line
                      type="monotone"
                      dataKey="value"
                      name="Sales"
                      stroke={theme.palette.primary.main}
                      strokeWidth={2}
                      dot={false}
                    />
                  </LineChart>
                </ResponsiveContainer>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 4 }}>
          <Card elevation={2}>
            <CardHeader title="Sales by Category" />
            <CardContent>
              <Box sx={{ width: "100%", height: 300 }}>
                <ResponsiveContainer>
                  <BarChart data={data.categoryBreakdown}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="label" />
                    <YAxis
                      tickFormatter={(v: number) =>
                        new Intl.NumberFormat("en-IN", { notation: "compact" }).format(v)
                      }
                    />
                    <RechartsTooltip
                      formatter={(value: number) => formatCurrency(value, c)}
                    />
                    <Bar dataKey="value" name="Sales" fill={theme.palette.secondary.main} />
                  </BarChart>
                </ResponsiveContainer>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12 }}>
          <Card elevation={2}>
            <CardHeader title="Low Stock Alerts" />
            <CardContent>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Product</TableCell>
                    <TableCell>SKU</TableCell>
                    <TableCell align="right">On Hand</TableCell>
                    <TableCell align="right">Reorder Level</TableCell>
                    <TableCell align="center">Status</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {data.lowStockItems.map((item) => (
                    <TableRow key={item.productId} hover>
                      <TableCell>{item.name}</TableCell>
                      <TableCell>{item.sku}</TableCell>
                      <TableCell align="right">{item.quantityOnHand}</TableCell>
                      <TableCell align="right">{item.reorderLevel}</TableCell>
                      <TableCell align="center">
                        <Chip
                          size="small"
                          color={item.quantityOnHand === 0 ? "error" : "warning"}
                          label={item.quantityOnHand === 0 ? "Out of stock" : "Low"}
                        />
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
}
