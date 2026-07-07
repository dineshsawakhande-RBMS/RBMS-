"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import type { PagedResult, SendWhatsAppRequest, WhatsAppMessage } from "@/types";

interface MessagesParams {
  page: number;
  pageSize: number;
}

export function useWhatsAppMessages(params: MessagesParams) {
  return useQuery({
    queryKey: ["whatsapp-messages", params],
    queryFn: async () =>
      (await apiClient.get<PagedResult<WhatsAppMessage>>("/whatsapp/messages", { params })).data,
    placeholderData: (prev) => prev,
  });
}

export function useSendWhatsApp() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: SendWhatsAppRequest) => (await apiClient.post<string>("/whatsapp/messages", body)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["whatsapp-messages"] }),
  });
}

export function useSendInvoiceWhatsApp() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (saleId: string) => (await apiClient.post<string>(`/whatsapp/sales/${saleId}/invoice`)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["whatsapp-messages"] }),
  });
}
