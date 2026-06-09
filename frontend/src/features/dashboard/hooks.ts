"use client";

import { useQuery, type UseQueryResult } from "@tanstack/react-query";
import type { DashboardSummary } from "@/types";
import { fetchDashboardSummary, mockDashboardSummary } from "./api";

export const dashboardKeys = {
  all: ["dashboard"] as const,
  summary: () => [...dashboardKeys.all, "summary"] as const,
};

/**
 * Loads the dashboard summary. Falls back to mock data if the API call fails
 * so the dashboard remains demonstrable before the backend is wired up.
 */
export function useDashboardSummary(): UseQueryResult<DashboardSummary> {
  return useQuery({
    queryKey: dashboardKeys.summary(),
    queryFn: async () => {
      try {
        return await fetchDashboardSummary();
      } catch {
        return mockDashboardSummary;
      }
    },
    staleTime: 30_000,
  });
}
