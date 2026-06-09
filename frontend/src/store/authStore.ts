import { create } from "zustand";
import { persist, createJSONStorage } from "zustand/middleware";
import type { AuthTokens, Role, User } from "@/types";

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  expiresAt: number | null;
  user: User | null;

  setTokens: (tokens: AuthTokens) => void;
  login: (tokens: AuthTokens, user: User) => void;
  logout: () => void;
  setUser: (user: User) => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      expiresAt: null,
      user: null,

      setTokens: (tokens) =>
        set({
          accessToken: tokens.accessToken,
          refreshToken: tokens.refreshToken,
          expiresAt: tokens.expiresAt,
        }),

      login: (tokens, user) =>
        set({
          accessToken: tokens.accessToken,
          refreshToken: tokens.refreshToken,
          expiresAt: tokens.expiresAt,
          user,
        }),

      logout: () =>
        set({
          accessToken: null,
          refreshToken: null,
          expiresAt: null,
          user: null,
        }),

      setUser: (user) => set({ user }),
    }),
    {
      name: "rbms-auth",
      storage: createJSONStorage(() => localStorage),
      // Do not persist nothing-sensitive beyond what we need to re-auth silently.
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        expiresAt: state.expiresAt,
        user: state.user,
      }),
    },
  ),
);

/**
 * Non-reactive accessors for use outside React (e.g. axios interceptors),
 * where calling the hook would be invalid.
 */
export const authStore = {
  getState: useAuthStore.getState,
  setState: useAuthStore.setState,
};

export const hasRole = (user: User | null, role: Role): boolean =>
  !!user?.roles.includes(role);

export const hasAnyRole = (user: User | null, roles: Role[]): boolean =>
  !!user?.roles.some((r) => roles.includes(r));
