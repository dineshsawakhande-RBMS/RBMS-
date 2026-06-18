"use client";

import { useQuery } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import type { PagedResult, ProductListItem } from "@/types";

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
