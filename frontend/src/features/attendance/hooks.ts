"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import type {
  AttendanceRecord, AttendanceSummary, CreateLeaveRequest, LeaveRequestItem,
  LeaveStatus, MarkAttendanceRequest, PagedResult,
} from "@/types";

export function useMonthlyAttendance(employeeId: string | null, year: number, month: number) {
  return useQuery({
    queryKey: ["attendance", employeeId, year, month],
    queryFn: async () =>
      (await apiClient.get<AttendanceRecord[]>("/attendance", { params: { employeeId, year, month } })).data,
    enabled: !!employeeId,
  });
}

export function useAttendanceSummary(employeeId: string | null, year: number, month: number, enabled = true) {
  return useQuery({
    queryKey: ["attendance-summary", employeeId, year, month],
    queryFn: async () =>
      (await apiClient.get<AttendanceSummary>("/attendance/summary", { params: { employeeId, year, month } })).data,
    enabled: enabled && !!employeeId,
  });
}

export function useMarkAttendance() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: MarkAttendanceRequest) => (await apiClient.post<number>("/attendance", body)).data,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["attendance"] });
      qc.invalidateQueries({ queryKey: ["attendance-summary"] });
    },
  });
}

interface LeavesParams {
  employeeId?: string;
  status?: LeaveStatus;
  page: number;
  pageSize: number;
}

export function useLeaves(params: LeavesParams) {
  return useQuery({
    queryKey: ["leaves", params],
    queryFn: async () =>
      (await apiClient.get<PagedResult<LeaveRequestItem>>("/leaves", { params })).data,
    placeholderData: (prev) => prev,
  });
}

export function useCreateLeave() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreateLeaveRequest) => (await apiClient.post<string>("/leaves", body)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["leaves"] }),
  });
}

export function useDecideLeave() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, approve, decisionNotes }: { id: string; approve: boolean; decisionNotes?: string }) => {
      await apiClient.post(`/leaves/${id}/decide`, { approve, decisionNotes: decisionNotes ?? null });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["leaves"] });
      qc.invalidateQueries({ queryKey: ["attendance"] });
      qc.invalidateQueries({ queryKey: ["attendance-summary"] });
    },
  });
}
