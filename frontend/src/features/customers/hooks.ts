"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import type { CreateCustomerRequest, CustomerDetail, CustomerListItem, PagedResult, UpdateCustomerRequest } from "@/types";

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

export function useCustomer(id: string | null) {
  return useQuery({
    queryKey: ["customer", id],
    queryFn: async () => (await apiClient.get<CustomerDetail>(`/customers/${id}`)).data,
    enabled: !!id,
  });
}

export function useUpdateCustomer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: UpdateCustomerRequest) => { await apiClient.put(`/customers/${body.id}`, body); },
    onSuccess: (_d, body) => {
      qc.invalidateQueries({ queryKey: ["customers"] });
      qc.invalidateQueries({ queryKey: ["customer", body.id] });
    },
  });
}

export function useDeleteCustomer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => { await apiClient.delete(`/customers/${id}`); },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["customers"] }),
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
