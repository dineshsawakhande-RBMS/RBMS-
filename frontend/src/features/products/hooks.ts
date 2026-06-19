"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import type { CreateProductRequest, PagedResult, ProductDetail, ProductListItem, UpdateProductRequest } from "@/types";

interface ProductsParams {
  search?: string;
  page: number;
  pageSize: number;
}

async function fetchProducts(params: ProductsParams): Promise<PagedResult<ProductListItem>> {
  const { data } = await apiClient.get<PagedResult<ProductListItem>>("/products", { params });
  return data;
}

export function useProducts(params: ProductsParams) {
  return useQuery({
    queryKey: ["products", params],
    queryFn: () => fetchProducts(params),
    placeholderData: (prev) => prev,
  });
}

export function useProduct(id: string | null) {
  return useQuery({
    queryKey: ["product", id],
    queryFn: async () => {
      const { data } = await apiClient.get<ProductDetail>(`/products/${id}`);
      return data;
    },
    enabled: !!id,
  });
}

export function useUpdateProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: UpdateProductRequest) => {
      await apiClient.put(`/products/${body.id}`, body);
    },
    onSuccess: (_d, body) => {
      qc.invalidateQueries({ queryKey: ["products"] });
      qc.invalidateQueries({ queryKey: ["product", body.id] });
    },
  });
}

export function useDeleteProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => { await apiClient.delete(`/products/${id}`); },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["products"] }),
  });
}

export function useCreateProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreateProductRequest) => {
      const { data } = await apiClient.post<string>("/products", body);
      return data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["products"] }),
  });
}
