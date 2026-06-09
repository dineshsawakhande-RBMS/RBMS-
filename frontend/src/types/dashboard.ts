export interface MetricPoint {
  /** ISO date or short label, e.g. "2026-06-01" or "Jun". */
  label: string;
  value: number;
}

export interface LowStockItem {
  productId: string;
  name: string;
  sku: string;
  quantityOnHand: number;
  reorderLevel: number;
}

export interface DashboardSummary {
  todaySales: number;
  monthlySales: number;
  monthlyProfit: number;
  inventoryValue: number;
  lowStockCount: number;
  pendingOrders: number;
  /** Currency code used to format monetary figures, e.g. "INR". */
  currency: string;
  salesTrend: MetricPoint[];
  categoryBreakdown: MetricPoint[];
  lowStockItems: LowStockItem[];
}
