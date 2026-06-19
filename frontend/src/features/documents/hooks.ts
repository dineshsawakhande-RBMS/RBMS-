"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/apiClient";
import type { DocumentDetail, DocumentListItem, DocumentType, PagedResult, UpdateDocumentRequest } from "@/types";

interface DocumentsParams {
  search?: string;
  documentType?: DocumentType;
  page: number;
  pageSize: number;
}

async function fetchDocuments(params: DocumentsParams): Promise<PagedResult<DocumentListItem>> {
  const { data } = await apiClient.get<PagedResult<DocumentListItem>>("/documents", { params });
  return data;
}

export function useDocuments(params: DocumentsParams) {
  return useQuery({
    queryKey: ["documents", params],
    queryFn: () => fetchDocuments(params),
    placeholderData: (prev) => prev,
  });
}

export function useDocument(id: string | null) {
  return useQuery({
    queryKey: ["document", id],
    queryFn: async () => (await apiClient.get<DocumentDetail>(`/documents/${id}`)).data,
    enabled: !!id,
  });
}

export function useExpiringDocuments(withinDays = 30) {
  return useQuery({
    queryKey: ["documents-expiring", withinDays],
    queryFn: async () =>
      (await apiClient.get<DocumentListItem[]>("/documents/expiring", { params: { withinDays } })).data,
  });
}

export interface UploadDocumentInput {
  file: File;
  title: string;
  documentType: DocumentType;
  description?: string;
  tags?: string;
  issueDate?: string;
  expiryDate?: string;
  relatedEntityType?: string;
  relatedEntityId?: string;
}

export function useUploadDocument() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (input: UploadDocumentInput) => {
      const form = new FormData();
      form.append("file", input.file);
      form.append("title", input.title);
      form.append("documentType", input.documentType);
      if (input.description) form.append("description", input.description);
      if (input.tags) form.append("tags", input.tags);
      if (input.issueDate) form.append("issueDate", input.issueDate);
      if (input.expiryDate) form.append("expiryDate", input.expiryDate);
      if (input.relatedEntityType) form.append("relatedEntityType", input.relatedEntityType);
      if (input.relatedEntityId) form.append("relatedEntityId", input.relatedEntityId);
      // Content-Type undefined → axios sets multipart/form-data with the boundary.
      const { data } = await apiClient.post<string>("/documents", form, { headers: { "Content-Type": undefined } });
      return data;
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["documents"] });
      qc.invalidateQueries({ queryKey: ["documents-expiring"] });
    },
  });
}

export function useUpdateDocument() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: UpdateDocumentRequest) => { await apiClient.put(`/documents/${body.id}`, body); },
    onSuccess: (_d, body) => {
      qc.invalidateQueries({ queryKey: ["documents"] });
      qc.invalidateQueries({ queryKey: ["document", body.id] });
      qc.invalidateQueries({ queryKey: ["documents-expiring"] });
    },
  });
}

export function useDeleteDocument() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => { await apiClient.delete(`/documents/${id}`); },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["documents"] });
      qc.invalidateQueries({ queryKey: ["documents-expiring"] });
    },
  });
}
