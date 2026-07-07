import { create } from "zustand";
import { persist, createJSONStorage } from "zustand/middleware";
import { DEFAULT_STORE_ID } from "@/lib/config";

interface StoreState {
  /** The store the user is currently operating on (drives store-scoped views). */
  activeStoreId: string | null;
  setActiveStoreId: (id: string) => void;
}

export const useStoreStore = create<StoreState>()(
  persist(
    (set) => ({
      activeStoreId: null,
      setActiveStoreId: (id) => set({ activeStoreId: id }),
    }),
    {
      name: "rbms-active-store",
      storage: createJSONStorage(() => localStorage),
    },
  ),
);

/** Reactive hook for the active store id (may be null before bootstrap). */
export const useActiveStoreId = () => useStoreStore((s) => s.activeStoreId);

/**
 * Always-valid store id for store-scoped queries: the active store once chosen, otherwise the
 * seeded default. Prevents null-store queries during first-load bootstrap.
 */
export const useEffectiveStoreId = (): string =>
  useStoreStore((s) => s.activeStoreId) ?? DEFAULT_STORE_ID;
