"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import type { CreateSupplierRequest, PagedResult, SupplierLedger, SupplierListItem } from "@/types";

interface SuppliersParams {
  search?: string;
  page: number;
  pageSize: number;
}

async function fetchSuppliers(params: SuppliersParams): Promise<PagedResult<SupplierListItem>> {
  const { data } = await apiClient.get<PagedResult<SupplierListItem>>("/suppliers", { params });
  return data;
}

export function useSuppliers(params: SuppliersParams) {
  return useQuery({
    queryKey: ["suppliers", params],
    queryFn: () => fetchSuppliers(params),
    placeholderData: (prev) => prev,
  });
}

export function useSupplierLedger(id: string | null) {
  return useQuery({
    queryKey: ["supplier-ledger", id],
    queryFn: async () => {
      const { data } = await apiClient.get<SupplierLedger>(`/suppliers/${id}/ledger`);
      return data;
    },
    enabled: !!id,
  });
}

export function useDeleteSupplier() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => { await apiClient.delete(`/suppliers/${id}`); },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["suppliers"] }),
  });
}

export function useCreateSupplier() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreateSupplierRequest) => {
      const { data } = await apiClient.post<string>("/suppliers", body);
      return data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["suppliers"] }),
  });
}
