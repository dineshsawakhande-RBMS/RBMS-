"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import type { CreateEmployeeRequest, EmployeeListItem, PagedResult } from "@/types";

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
