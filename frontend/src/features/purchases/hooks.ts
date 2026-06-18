"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import type { CreatePurchaseRequest, PagedResult, PurchaseListItem } from "@/types";

interface PurchasesParams {
  page: number;
  pageSize: number;
}

async function fetchPurchases(params: PurchasesParams): Promise<PagedResult<PurchaseListItem>> {
  const { data } = await apiClient.get<PagedResult<PurchaseListItem>>("/purchases", { params });
  return data;
}

export function usePurchases(params: PurchasesParams) {
  return useQuery({
    queryKey: ["purchases", params],
    queryFn: () => fetchPurchases(params),
    placeholderData: (prev) => prev,
  });
}

export function useCreatePurchase() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreatePurchaseRequest) => {
      const { data } = await apiClient.post<string>("/purchases", body);
      return data;
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["purchases"] });
      qc.invalidateQueries({ queryKey: ["stock-levels"] });
      qc.invalidateQueries({ queryKey: ["suppliers"] });
    },
  });
}
