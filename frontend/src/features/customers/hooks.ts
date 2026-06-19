"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import type { CreateCustomerRequest, CustomerListItem, PagedResult } from "@/types";

interface CustomersParams {
  search?: string;
  page: number;
  pageSize: number;
}

async function fetchCustomers(params: CustomersParams): Promise<PagedResult<CustomerListItem>> {
  const { data } = await apiClient.get<PagedResult<CustomerListItem>>("/customers", { params });
  return data;
}

export function useCustomers(params: CustomersParams) {
  return useQuery({
    queryKey: ["customers", params],
    queryFn: () => fetchCustomers(params),
    placeholderData: (prev) => prev,
  });
}

export function useCreateCustomer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreateCustomerRequest) => {
      const { data } = await apiClient.post<string>("/customers", body);
      return data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["customers"] }),
  });
}
