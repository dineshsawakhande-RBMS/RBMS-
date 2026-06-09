import apiClient from "@/lib/apiClient";
import type { DashboardSummary } from "@/types";

/**
 * Mock fallback used when the backend is unreachable (local dev before the
 * API is up, or Storybook-style previews). Mirrors the real response shape.
 */
export const mockDashboardSummary: DashboardSummary = {
  todaySales: 48250,
  monthlySales: 1284300,
  monthlyProfit: 312500,
  inventoryValue: 4875000,
  lowStockCount: 7,
  pendingOrders: 12,
  currency: "INR",
  salesTrend: [
    { label: "Jan", value: 980000 },
    { label: "Feb", value: 1050000 },
    { label: "Mar", value: 1120000 },
    { label: "Apr", value: 1190000 },
    { label: "May", value: 1240000 },
    { label: "Jun", value: 1284300 },
  ],
  categoryBreakdown: [
    { label: "Groceries", value: 420000 },
    { label: "Electronics", value: 360000 },
    { label: "Apparel", value: 280000 },
    { label: "Home", value: 224300 },
  ],
  lowStockItems: [
    { productId: "p-101", name: "Basmati Rice 5kg", sku: "GRC-RICE-5K", quantityOnHand: 4, reorderLevel: 20 },
    { productId: "p-102", name: "USB-C Charger 65W", sku: "ELC-CHG-65", quantityOnHand: 2, reorderLevel: 15 },
    { productId: "p-103", name: "Cotton T-Shirt (M)", sku: "APP-TSH-M", quantityOnHand: 6, reorderLevel: 25 },
  ],
};

export async function fetchDashboardSummary(): Promise<DashboardSummary> {
  const { data } = await apiClient.get<DashboardSummary>("/dashboard/summary");
  return data;
}
