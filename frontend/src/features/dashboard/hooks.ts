"use client";

import { useQuery, type UseQueryResult } from "@tanstack/react-query";
import type { DashboardSummary } from "@/types";
import { fetchDashboardSummary } from "./api";

export const dashboardKeys = {
  all: ["dashboard"] as const,
  summary: () => [...dashboardKeys.all, "summary"] as const,
};

export function useDashboardSummary(): UseQueryResult<DashboardSummary> {
  return useQuery({
    queryKey: dashboardKeys.summary(),
    queryFn: fetchDashboardSummary,
    staleTime: 30_000,
  });
}
