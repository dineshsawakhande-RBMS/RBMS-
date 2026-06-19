"use client";

import { useQuery } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";

export type ReportType = "sales" | "purchases" | "inventory" | "profit";

export interface ReportParams {
  from?: string;
  to?: string;
  storeId?: string;
}

// Loosely typed report payloads (shape varies by type; the page renders per type).
export interface ReportData {
  rows: Record<string, unknown>[];
  [key: string]: unknown;
}

async function fetchReport(type: ReportType, params: ReportParams): Promise<ReportData> {
  const { data } = await apiClient.get<ReportData>(`/reports/${type}`, { params });
  return data;
}

export function useReport(type: ReportType, params: ReportParams) {
  return useQuery({
    queryKey: ["report", type, params],
    queryFn: () => fetchReport(type, params),
    placeholderData: (prev) => prev,
  });
}

/** Downloads the report as CSV or Excel (authenticated via the axios client). */
export async function downloadReport(type: ReportType, params: ReportParams, format: "csv" | "xlsx") {
  const res = await apiClient.get(`/reports/${type}`, {
    params: { ...params, format },
    responseType: "blob",
  });
  saveBlob(res.data as Blob, `${type}-report.${format}`);
}

export function saveBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}
