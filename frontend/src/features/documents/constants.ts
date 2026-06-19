import type { DocumentType } from "@/types";

/** Document types with display labels (order shown in dropdowns). */
export const DOCUMENT_TYPES: { value: DocumentType; label: string }[] = [
  { value: "GstCertificate", label: "GST Certificate" },
  { value: "RentAgreement", label: "Rent Agreement" },
  { value: "License", label: "License" },
  { value: "Insurance", label: "Insurance" },
  { value: "Contract", label: "Contract" },
  { value: "SupplierDocument", label: "Supplier Document" },
  { value: "EmployeeDocument", label: "Employee Document" },
  { value: "Invoice", label: "Invoice" },
  { value: "BankStatement", label: "Bank Statement" },
  { value: "Other", label: "Other" },
];

const LABELS = Object.fromEntries(DOCUMENT_TYPES.map((t) => [t.value, t.label])) as Record<DocumentType, string>;

export function documentTypeLabel(type: DocumentType | string): string {
  return LABELS[type as DocumentType] ?? type;
}

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
