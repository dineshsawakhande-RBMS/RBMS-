import { create } from "zustand";
import { persist, createJSONStorage } from "zustand/middleware";
import type { AuthResult, AuthTokens, AuthUser, Role } from "@/types";

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  expiresAt: string | null;
  user: AuthUser | null;

  /** Store the full login/refresh result (tokens + user). */
  setSession: (result: AuthResult) => void;
  /** Update just the tokens (used by the silent refresh flow). */
  setTokens: (tokens: AuthTokens) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      expiresAt: null,
      user: null,

      setSession: (r) =>
        set({
          accessToken: r.accessToken,
          refreshToken: r.refreshToken,
          expiresAt: r.accessTokenExpiresAt,
          user: { userId: r.userId, username: r.username, fullName: r.fullName, roles: r.roles },
        }),

      setTokens: (t) =>
        set({
          accessToken: t.accessToken,
          refreshToken: t.refreshToken,
          expiresAt: t.accessTokenExpiresAt,
        }),

      logout: () => set({ accessToken: null, refreshToken: null, expiresAt: null, user: null }),
    }),
    {
      name: "rbms-auth",
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        expiresAt: state.expiresAt,
        user: state.user,
      }),
    },
  ),
);

/** Non-reactive accessor for use outside React (e.g. the axios interceptor). */
export const authStore = {
  getState: useAuthStore.getState,
  setState: useAuthStore.setState,
};

export const hasRole = (user: AuthUser | null, role: Role): boolean =>
  !!user?.roles.includes(role);

export const hasAnyRole = (user: AuthUser | null, roles: Role[]): boolean =>
  !!user?.roles.some((r) => roles.includes(r));
