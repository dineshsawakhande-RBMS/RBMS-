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

export function formatNumber(value: number | null | undefined): string {
  const n = typeof value === "number" ? value : 0;
  return new Intl.NumberFormat("en-IN").format(n);
}
