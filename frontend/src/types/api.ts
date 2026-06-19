/** Mirrors the backend PagedResult<T>. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

// ---- Products ----
export interface ProductListItem {
  id: string;
  name: string;
  brandName: string | null;
  categoryName: string | null;
  gstRate: number;
  variantCount: number;
  isActive: boolean;
}

export interface ProductVariant {
  id: string;
  sku: string;
  barcode: string | null;
  size: string | null;
  color: string | null;
  purchasePrice: number;
  sellingPrice: number;
  mrp: number | null;
  reorderLevel: number;
  isActive: boolean;
}

export interface CreateProductVariantInput {
  sku: string;
  barcode?: string | null;
  size?: string | null;
  color?: string | null;
  purchasePrice: number;
  sellingPrice: number;
  mrp?: number | null;
  reorderLevel: number;
}

export interface CreateProductRequest {
  name: string;
  description?: string | null;
  hsnCode?: string | null;
  gstRate: number;
  categoryId?: string | null;
  brandId?: string | null;
  variants: CreateProductVariantInput[];
}

export interface ProductDetail {
  id: string;
  name: string;
  description: string | null;
  hsnCode: string | null;
  gstRate: number;
  categoryName: string | null;
  brandName: string | null;
  isActive: boolean;
  variants: ProductVariant[];
}

// ---- Inventory ----
export interface StockLevel {
  variantId: string;
  sku: string;
  productName: string;
  size: string | null;
  color: string | null;
  quantityOnHand: number;
  reorderLevel: number;
  avgCost: number;
  stockValue: number;
  isLow: boolean;
  sellingPrice: number;
}

export interface UpdateProductRequest {
  id: string;
  name: string;
  description?: string | null;
  hsnCode?: string | null;
  gstRate: number;
  categoryId?: string | null;
  brandId?: string | null;
  isActive: boolean;
}

// ---- Suppliers ----
export interface SupplierListItem {
  id: string;
  code: string;
  name: string;
  phone: string | null;
  gstin: string | null;
  outstandingBalance: number;
  isActive: boolean;
}

export interface SupplierLedgerEntry {
  entryDate: string;
  referenceType: string;
  debit: number;
  credit: number;
  runningBalance: number;
  notes: string | null;
}

export interface SupplierLedger {
  supplierId: string;
  name: string;
  outstanding: number;
  entries: SupplierLedgerEntry[];
}

export interface CreateSupplierRequest {
  code: string;
  name: string;
  gstin?: string | null;
  contactPerson?: string | null;
  phone?: string | null;
  email?: string | null;
  addressLine1?: string | null;
  city?: string | null;
  state?: string | null;
  pincode?: string | null;
  paymentTermsDays: number;
  openingBalance: number;
}

// ---- Purchases ----
export interface PurchaseListItem {
  id: string;
  supplierName: string;
  invoiceNumber: string | null;
  invoiceDate: string;
  grandTotal: number;
  amountPaid: number;
  paymentStatus: string;
}

export interface CreatePurchaseItem {
  variantId: string;
  quantity: number;
  unitCost: number;
  gstRate: number;
}

export interface CreatePurchaseRequest {
  supplierId: string;
  storeId: string;
  invoiceNumber?: string | null;
  invoiceDate: string;
  discount: number;
  amountPaid: number;
  notes?: string | null;
  items: CreatePurchaseItem[];
}

// ---- Customers ----
export interface CustomerListItem {
  id: string;
  name: string;
  mobile: string;
  email: string | null;
  loyaltyPoints: number;
  isActive: boolean;
}

export interface CreateCustomerRequest {
  name: string;
  mobile: string;
  email?: string | null;
  addressLine1?: string | null;
  city?: string | null;
  state?: string | null;
  pincode?: string | null;
  birthday?: string | null;
  anniversary?: string | null;
}

// ---- Sales ----
export type PaymentMethod = "Cash" | "Card" | "UPI" | "BankTransfer" | "Wallet" | "StoreCredit" | "Cheque";

export interface SaleListItem {
  id: string;
  invoiceNumber: string;
  invoiceDate: string;
  grandTotal: number;
  status: string;
  paymentStatus: string;
}

export interface SaleItemDetail {
  variantId: string;
  sku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  gstRate: number;
  taxAmount: number;
  lineTotal: number;
}

export interface SaleDetail {
  id: string;
  invoiceNumber: string;
  invoiceDate: string;
  storeId: string;
  customerId: string | null;
  status: string;
  subtotal: number;
  discount: number;
  cgst: number;
  sgst: number;
  grandTotal: number;
  amountPaid: number;
  changeDue: number;
  paymentStatus: string;
  items: SaleItemDetail[];
  payments: { method: PaymentMethod; amount: number; reference: string | null }[];
}

export interface CreateSaleReturnRequest {
  saleId: string;
  storeId: string;
  reason?: string | null;
  refundMethod?: PaymentMethod | null;
  items: { variantId: string; quantity: number; unitPrice: number }[];
}

export interface CreateSaleItem {
  variantId: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  gstRate: number;
}

export interface CreateSalePayment {
  method: PaymentMethod;
  amount: number;
  reference?: string | null;
}

export interface CreateSaleRequest {
  storeId: string;
  customerId?: string | null;
  discount: number;
  items: CreateSaleItem[];
  payments: CreateSalePayment[];
  notes?: string | null;
}
