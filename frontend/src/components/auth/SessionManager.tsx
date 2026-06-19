"use client";

import { useEffect } from "react";
import axios from "axios";
import { useAuthStore } from "@/store/authStore";
import type { AuthResult } from "@/types";

const BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "") ?? "http://localhost:5080";

/**
 * Proactively refreshes the JWT access token shortly before it expires, so the user is
 * never bounced to the login screen mid-session. Renders nothing. Mounted inside the
 * authenticated shell. The axios 401-retry flow remains as a safety net.
 */
export default function SessionManager() {
  const expiresAt = useAuthStore((s) => s.expiresAt);
  const refreshToken = useAuthStore((s) => s.refreshToken);

  useEffect(() => {
    if (!expiresAt || !refreshToken) return;

    const msUntilExpiry = new Date(expiresAt).getTime() - Date.now();
    // Refresh 60s early; if already within that window, refresh almost immediately.
    const delay = Math.max(2_000, msUntilExpiry - 60_000);

    const timer = setTimeout(async () => {
      try {
        const { data } = await axios.post<AuthResult>(
          `${BASE_URL}/api/auth/refresh`,
          { refreshToken },
          { headers: { "Content-Type": "application/json" } },
        );
        useAuthStore.getState().setTokens(data);
      } catch {
        // Refresh failed (e.g. refresh token revoked/expired): let the next 401 + the
        // RouteGuard handle redirecting to login.
      }
    }, delay);

    return () => clearTimeout(timer);
  }, [expiresAt, refreshToken]);

  return null;
}
