import axios, {
  AxiosError,
  type AxiosInstance,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from "axios";
import { authStore } from "@/store/authStore";
import type { AuthTokens } from "@/types";

const BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "") ?? "http://localhost:8080";
const TIMEOUT = Number(process.env.NEXT_PUBLIC_API_TIMEOUT_MS ?? 15000);

export const apiClient: AxiosInstance = axios.create({
  baseURL: `${BASE_URL}/api`,
  timeout: TIMEOUT,
  headers: { "Content-Type": "application/json" },
});

// --- Request interceptor: attach the current JWT access token. ---
apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = authStore.getState().accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// --- 401 -> single-flight refresh-token retry flow. ---
interface RetriableConfig extends AxiosRequestConfig {
  _retry?: boolean;
}

let refreshPromise: Promise<AuthTokens> | null = null;

async function refreshAccessToken(): Promise<AuthTokens> {
  const { refreshToken } = authStore.getState();
  if (!refreshToken) {
    throw new Error("No refresh token available");
  }

  // Use a bare axios call so we don't recurse through the interceptors.
  const { data } = await axios.post<AuthTokens>(
    `${BASE_URL}/api/auth/refresh`,
    { refreshToken },
    { timeout: TIMEOUT, headers: { "Content-Type": "application/json" } },
  );

  authStore.getState().setTokens(data);
  return data;
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as (RetriableConfig & InternalAxiosRequestConfig) | undefined;

    const isUnauthorized = error.response?.status === 401;
    const canRetry = !!original && !original._retry && !!authStore.getState().refreshToken;

    if (isUnauthorized && canRetry) {
      original._retry = true;
      try {
        // Coalesce concurrent 401s onto a single refresh request.
        refreshPromise ??= refreshAccessToken().finally(() => {
          refreshPromise = null;
        });
        const tokens = await refreshPromise;
        original.headers = original.headers ?? {};
        original.headers.Authorization = `Bearer ${tokens.accessToken}`;
        return apiClient(original);
      } catch (refreshError) {
        authStore.getState().logout();
        if (typeof window !== "undefined") {
          window.location.href = "/login";
        }
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  },
);

export default apiClient;
