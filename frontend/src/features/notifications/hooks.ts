"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import type { NotificationItem, PagedResult, RefreshNotificationsResult } from "@/types";

export function useUnreadNotificationCount() {
  return useQuery({
    queryKey: ["notifications-count"],
    queryFn: async () => (await apiClient.get<number>("/notifications/count")).data,
    refetchInterval: 60_000,           // keep the badge fresh
    refetchOnWindowFocus: true,
  });
}

export function useNotifications(enabled: boolean) {
  return useQuery({
    queryKey: ["notifications"],
    queryFn: async () =>
      (await apiClient.get<PagedResult<NotificationItem>>("/notifications", { params: { pageSize: 50 } })).data,
    enabled,
  });
}

export function useRefreshNotifications() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async () => (await apiClient.post<RefreshNotificationsResult>("/notifications/refresh")).data,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["notifications"] });
      qc.invalidateQueries({ queryKey: ["notifications-count"] });
    },
  });
}

export function useMarkNotificationRead() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => { await apiClient.post(`/notifications/${id}/read`); },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["notifications"] });
      qc.invalidateQueries({ queryKey: ["notifications-count"] });
    },
  });
}

export function useMarkAllNotificationsRead() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async () => (await apiClient.post<number>("/notifications/read-all")).data,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["notifications"] });
      qc.invalidateQueries({ queryKey: ["notifications-count"] });
    },
  });
}
