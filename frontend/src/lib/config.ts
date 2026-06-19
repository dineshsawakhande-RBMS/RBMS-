/**
 * App configuration. For the single-store phase we default to the seeded store;
 * multi-store will replace this with a store switcher driven by the user's stores.
 */
export const DEFAULT_STORE_ID =
  process.env.NEXT_PUBLIC_DEFAULT_STORE_ID ?? "aaaaaaaa-0000-0000-0000-000000000002";

export const CURRENCY = process.env.NEXT_PUBLIC_CURRENCY ?? "INR";

export function formatMoney(value: number | null | undefined): string {
  const n = typeof value === "number" ? value : 0;
  return new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency: CURRENCY,
    maximumFractionDigits: 0,
  }).format(n);
}

/** Absolute URL for a server media path (e.g. "/uploads/products/x.jpg"). */
export function mediaUrl(path: string): string {
  const base = process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "") ?? "http://localhost:5080";
  return path.startsWith("http") ? path : `${base}${path}`;
}

export function formatNumber(value: number | null | undefined): string {
  const n = typeof value === "number" ? value : 0;
  return new Intl.NumberFormat("en-IN").format(n);
}
