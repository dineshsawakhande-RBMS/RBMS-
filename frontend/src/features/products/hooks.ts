"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import type { CreateProductRequest, PagedResult, ProductDetail, ProductImage, ProductListItem, UpdateProductRequest } from "@/types";

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

export function useProductImages(productId: string | null) {
  return useQuery({
    queryKey: ["product-images", productId],
    queryFn: async () => (await apiClient.get<ProductImage[]>(`/products/${productId}/images`)).data,
    enabled: !!productId,
  });
}

export function useUploadProductImage() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ productId, file }: { productId: string; file: File }) => {
      const form = new FormData();
      form.append("file", file);
      form.append("isPrimary", "false");
      // Content-Type undefined → axios sets multipart/form-data with the boundary.
      await apiClient.post(`/products/${productId}/images`, form, { headers: { "Content-Type": undefined } });
    },
    onSuccess: (_d, v) => qc.invalidateQueries({ queryKey: ["product-images", v.productId] }),
  });
}

export function useDeleteProductImage() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ imageId }: { imageId: string; productId: string }) => {
      await apiClient.delete(`/products/images/${imageId}`);
    },
    onSuccess: (_d, v) => qc.invalidateQueries({ queryKey: ["product-images", v.productId] }),
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
