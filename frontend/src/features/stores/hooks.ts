"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import type { CreateStoreRequest, StoreDetail, StoreListItem, TransferStockRequest, UpdateStoreRequest } from "@/types";

export function useStores() {
  return useQuery({
    queryKey: ["stores"],
    queryFn: async () => (await apiClient.get<StoreListItem[]>("/stores")).data,
    staleTime: 5 * 60_000,
  });
}

export function useStore(id: string | null) {
  return useQuery({
    queryKey: ["store", id],
    queryFn: async () => (await apiClient.get<StoreDetail>(`/stores/${id}`)).data,
    enabled: !!id,
  });
}

export function useCreateStore() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreateStoreRequest) => (await apiClient.post<string>("/stores", body)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["stores"] }),
  });
}

export function useUpdateStore() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: UpdateStoreRequest) => { await apiClient.put(`/stores/${body.id}`, body); },
    onSuccess: (_d, body) => {
      qc.invalidateQueries({ queryKey: ["stores"] });
      qc.invalidateQueries({ queryKey: ["store", body.id] });
    },
  });
}

export function useDeleteStore() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => { await apiClient.delete(`/stores/${id}`); },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["stores"] }),
  });
}

export function useTransferStock() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: TransferStockRequest) => (await apiClient.post<string>("/inventory/transfers", body)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["stock-levels"] }),
  });
}
