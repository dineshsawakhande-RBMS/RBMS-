export interface TopProduct {
  productId: string;
  name: string;
  quantitySold: number;
  revenue: number;
}

/** Mirrors the backend DashboardSummaryDto. */
export interface DashboardSummary {
  todaySales: number;
  monthlySales: number;
  purchaseCost: number;
  profit: number;
  inventoryValue: number;
  productCount: number;
  lowStockCount: number;
  employeeCount: number;
  pendingSalaries: number;
  monthlyExpenses: number;
  cashFlow: number;
  topSellingProducts: TopProduct[];
}
