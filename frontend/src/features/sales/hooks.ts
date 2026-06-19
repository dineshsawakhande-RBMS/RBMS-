"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import type { CreateSaleRequest, CreateSaleReturnRequest, PagedResult, SaleDetail, SaleListItem } from "@/types";

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

export function useSale(id: string | null) {
  return useQuery({
    queryKey: ["sale", id],
    queryFn: async () => {
      const { data } = await apiClient.get<SaleDetail>(`/sales/${id}`);
      return data;
    },
    enabled: !!id,
  });
}

export function useCreateSaleReturn() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreateSaleReturnRequest) => {
      const { data } = await apiClient.post<string>("/sales/returns", body);
      return data;
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["sales"] });
      qc.invalidateQueries({ queryKey: ["stock-levels"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
    },
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
