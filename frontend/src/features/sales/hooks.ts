"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import type { CreateSaleRequest, PagedResult, SaleListItem } from "@/types";

interface SalesParams {
  page: number;
  pageSize: number;
}

async function fetchSales(params: SalesParams): Promise<PagedResult<SaleListItem>> {
  const { data } = await apiClient.get<PagedResult<SaleListItem>>("/sales", { params });
  return data;
}

export function useSales(params: SalesParams) {
  return useQuery({
    queryKey: ["sales", params],
    queryFn: () => fetchSales(params),
    placeholderData: (prev) => prev,
  });
}

export function useCreateSale() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreateSaleRequest) => {
      const { data } = await apiClient.post<string>("/sales", body);
      return data;
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["sales"] });
      qc.invalidateQueries({ queryKey: ["stock-levels"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });
}
