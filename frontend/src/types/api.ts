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
