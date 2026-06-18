"use client";

import { useQuery } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import { DEFAULT_STORE_ID } from "@/lib/config";
import type { PagedResult, StockLevel } from "@/types";

interface StockParams {
  search?: string;
  lowStockOnly?: boolean;
  page: number;
  pageSize: number;
}

async function fetchStockLevels(params: StockParams): Promise<PagedResult<StockLevel>> {
  const { data } = await apiClient.get<PagedResult<StockLevel>>("/inventory/levels", {
    params: { storeId: DEFAULT_STORE_ID, ...params },
  });
  return data;
}

export function useStockLevels(params: StockParams) {
  return useQuery({
    queryKey: ["stock-levels", params],
    queryFn: () => fetchStockLevels(params),
    placeholderData: (prev) => prev,
  });
}
