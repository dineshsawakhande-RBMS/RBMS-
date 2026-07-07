"use client";

import { useQuery } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import type { PagedResult, StockLevel } from "@/types";

interface StockParams {
  search?: string;
  lowStockOnly?: boolean;
  page: number;
  pageSize: number;
}

async function fetchStockLevels(storeId: string, params: StockParams): Promise<PagedResult<StockLevel>> {
  const { data } = await apiClient.get<PagedResult<StockLevel>>("/inventory/levels", {
    params: { storeId, ...params },
  });
  return data;
}

export function useStockLevels(storeId: string, params: StockParams) {
  return useQuery({
    queryKey: ["stock-levels", storeId, params],
    queryFn: () => fetchStockLevels(storeId, params),
    placeholderData: (prev) => prev,
  });
}
