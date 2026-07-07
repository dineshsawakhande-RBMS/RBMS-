"use client";

import { useQuery } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import type { CustomerRetentionReport, DeadStockReport } from "@/types";

export function useDeadStock(storeId: string, days: number, slowThreshold: number) {
  return useQuery({
    queryKey: ["analytics-dead-stock", storeId, days, slowThreshold],
    queryFn: async () =>
      (await apiClient.get<DeadStockReport>("/analytics/dead-stock", {
        params: { storeId, days, slowThreshold },
      })).data,
    placeholderData: (prev) => prev,
  });
}

export function useCustomerRetention(months: number) {
  return useQuery({
    queryKey: ["analytics-retention", months],
    queryFn: async () =>
      (await apiClient.get<CustomerRetentionReport>("/analytics/customer-retention", {
        params: { months },
      })).data,
    placeholderData: (prev) => prev,
  });
}
