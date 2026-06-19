"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import type { CreateAdvanceRequest, GeneratePayrollRequest, PayrollListItem, SalaryAdvance } from "@/types";

export function usePayrolls(year: number, month: number) {
  return useQuery({
    queryKey: ["payrolls", year, month],
    queryFn: async () => (await apiClient.get<PayrollListItem[]>("/payroll", { params: { year, month } })).data,
  });
}

export function useGeneratePayroll() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: GeneratePayrollRequest) => (await apiClient.post<string>("/payroll/generate", body)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["payrolls"] }),
  });
}

export function useMarkPayrollPaid() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => { await apiClient.post(`/payroll/${id}/pay`); },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["payrolls"] }),
  });
}

export function useAdvances() {
  return useQuery({
    queryKey: ["advances"],
    queryFn: async () => (await apiClient.get<SalaryAdvance[]>("/payroll/advances")).data,
  });
}

export function useCreateAdvance() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreateAdvanceRequest) => (await apiClient.post<string>("/payroll/advances", body)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["advances"] }),
  });
}

export async function downloadSlip(id: string, label: string) {
  const res = await apiClient.get(`/payroll/${id}/slip`, { responseType: "blob" });
  const url = URL.createObjectURL(res.data as Blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `salary-slip-${label}.pdf`;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}
