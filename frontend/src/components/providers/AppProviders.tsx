"use client";

import { useState, useMemo, useEffect, createContext, useContext, type ReactNode } from "react";
import { AppRouterCacheProvider } from "@mui/material-nextjs/v15-appRouter";
import { ThemeProvider } from "@mui/material/styles";
import CssBaseline from "@mui/material/CssBaseline";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { getTheme, type ThemeMode } from "@/theme/theme";
import { ToastProvider } from "@/components/providers/ToastProvider";

interface ColorModeContextValue {
  mode: ThemeMode;
  toggleColorMode: () => void;
}

const ColorModeContext = createContext<ColorModeContextValue>({
  mode: "light",
  toggleColorMode: () => undefined,
});

export const useColorMode = () => useContext(ColorModeContext);

function makeQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 60_000,
        refetchOnWindowFocus: false,
        retry: 1,
      },
    },
  });
}

export function AppProviders({ children }: { children: ReactNode }) {
  // One QueryClient per browser session; created lazily so it isn't shared
  // across requests during SSR.
  const [queryClient] = useState(makeQueryClient);
  const [mode, setMode] = useState<ThemeMode>("light");

  // Restore the saved color mode after mount (avoids SSR hydration mismatch).
  useEffect(() => {
    const saved = window.localStorage.getItem("rbms-color-mode");
    if (saved === "light" || saved === "dark") setMode(saved);
  }, []);

  const colorMode = useMemo<ColorModeContextValue>(
    () => ({
      mode,
      toggleColorMode: () =>
        setMode((prev) => {
          const next = prev === "light" ? "dark" : "light";
          window.localStorage.setItem("rbms-color-mode", next);
          return next;
        }),
    }),
    [mode],
  );

  const theme = useMemo(() => getTheme(mode), [mode]);

  return (
    <AppRouterCacheProvider options={{ key: "mui" }}>
      <QueryClientProvider client={queryClient}>
        <ColorModeContext.Provider value={colorMode}>
          <ThemeProvider theme={theme}>
            <CssBaseline />
            <ToastProvider>{children}</ToastProvider>
          </ThemeProvider>
        </ColorModeContext.Provider>
        {process.env.NODE_ENV === "development" ? (
          <ReactQueryDevtools initialIsOpen={false} />
        ) : null}
      </QueryClientProvider>
    </AppRouterCacheProvider>
  );
}
