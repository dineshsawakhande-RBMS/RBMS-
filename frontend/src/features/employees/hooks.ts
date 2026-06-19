"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import type { CreateEmployeeRequest, EmployeeDetail, EmployeeListItem, PagedResult, UpdateEmployeeRequest } from "@/types";

interface EmployeesParams {
  search?: string;
  page: number;
  pageSize: number;
}

async function fetchEmployees(params: EmployeesParams): Promise<PagedResult<EmployeeListItem>> {
  const { data } = await apiClient.get<PagedResult<EmployeeListItem>>("/employees", { params });
  return data;
}

export function useEmployees(params: EmployeesParams) {
  return useQuery({
    queryKey: ["employees", params],
    queryFn: () => fetchEmployees(params),
    placeholderData: (prev) => prev,
  });
}

export function useEmployee(id: string | null) {
  return useQuery({
    queryKey: ["employee", id],
    queryFn: async () => (await apiClient.get<EmployeeDetail>(`/employees/${id}`)).data,
    enabled: !!id,
  });
}

export function useUpdateEmployee() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: UpdateEmployeeRequest) => { await apiClient.put(`/employees/${body.id}`, body); },
    onSuccess: (_d, body) => {
      qc.invalidateQueries({ queryKey: ["employees"] });
      qc.invalidateQueries({ queryKey: ["employee", body.id] });
    },
  });
}

export function useDeleteEmployee() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => { await apiClient.delete(`/employees/${id}`); },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["employees"] }),
  });
}

export function useCreateEmployee() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreateEmployeeRequest) => {
      const { data } = await apiClient.post<string>("/employees", body);
      return data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["employees"] }),
  });
}
