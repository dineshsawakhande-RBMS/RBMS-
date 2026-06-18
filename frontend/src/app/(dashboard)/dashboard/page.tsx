"use client";

import Grid from "@mui/material/Grid2";
import Box from "@mui/material/Box";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import CardHeader from "@mui/material/CardHeader";
import Typography from "@mui/material/Typography";
import Skeleton from "@mui/material/Skeleton";
import Alert from "@mui/material/Alert";
import { useTheme } from "@mui/material/styles";
import PointOfSaleIcon from "@mui/icons-material/PointOfSale";
import TrendingUpIcon from "@mui/icons-material/TrendingUp";
import SavingsIcon from "@mui/icons-material/Savings";
import Inventory2Icon from "@mui/icons-material/Inventory2";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import CategoryIcon from "@mui/icons-material/Category";
import {
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
} from "recharts";
import { StatCard } from "@/components/dashboard/StatCard";
import { useDashboardSummary } from "@/features/dashboard/hooks";
import { formatMoney, formatNumber } from "@/lib/config";

export default function DashboardPage() {
  const theme = useTheme();
  const { data, isLoading, isError } = useDashboardSummary();

  if (isError) {
    return (
      <Box>
        <Typography variant="h1" gutterBottom>
          Dashboard
        </Typography>
        <Alert severity="error">
          Could not load the dashboard. Make sure the API is running at the configured base URL.
        </Alert>
      </Box>
    );
  }

  if (isLoading || !data) {
    return (
      <Grid container spacing={3}>
        {Array.from({ length: 6 }).map((_, i) => (
          <Grid key={i} size={{ xs: 12, sm: 6, md: 4 }}>
            <Skeleton variant="rounded" height={120} />
          </Grid>
        ))}
        <Grid size={{ xs: 12 }}>
          <Skeleton variant="rounded" height={320} />
        </Grid>
      </Grid>
    );
  }

  const stats = [
    { title: "Today's Sales", value: formatMoney(data.todaySales), icon: <PointOfSaleIcon />, color: theme.palette.primary.main },
    { title: "Monthly Sales", value: formatMoney(data.monthlySales), icon: <TrendingUpIcon />, color: theme.palette.secondary.main },
    { title: "Profit", value: formatMoney(data.profit), icon: <SavingsIcon />, color: theme.palette.success.main },
    { title: "Inventory Value", value: formatMoney(data.inventoryValue), icon: <Inventory2Icon />, color: theme.palette.info.main },
    { title: "Active Products", value: formatNumber(data.productCount), icon: <CategoryIcon />, color: theme.palette.primary.dark },
    { title: "Low Stock Items", value: formatNumber(data.lowStockCount), icon: <WarningAmberIcon />, color: theme.palette.warning.main, subtitle: "below reorder level" },
  ];

  return (
    <Box>
      <Typography variant="h1" gutterBottom>
        Dashboard
      </Typography>

      <Alert severity="info" sx={{ mb: 3 }}>
        Inventory and product figures are live. Sales, profit and expense figures populate
        once the Sales &amp; Expense modules are in use.
      </Alert>

      <Grid container spacing={3}>
        {stats.map((s) => (
          <Grid key={s.title} size={{ xs: 12, sm: 6, md: 4 }}>
            <StatCard {...s} />
          </Grid>
        ))}

        <Grid size={{ xs: 12 }}>
          <Card elevation={2}>
            <CardHeader title="Top Selling Products" subheader="By revenue" />
            <CardContent>
              {data.topSellingProducts.length === 0 ? (
                <Typography variant="body2" color="text.secondary" sx={{ py: 4, textAlign: "center" }}>
                  No sales recorded yet.
                </Typography>
              ) : (
                <Box sx={{ width: "100%", height: 320 }}>
                  <ResponsiveContainer>
                    <BarChart data={data.topSellingProducts}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="name" />
                      <YAxis tickFormatter={(v: number) => new Intl.NumberFormat("en-IN", { notation: "compact" }).format(v)} />
                      <RechartsTooltip formatter={(value: number) => formatMoney(value)} />
                      <Bar dataKey="revenue" name="Revenue" fill={theme.palette.primary.main} />
                    </BarChart>
                  </ResponsiveContainer>
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
}
