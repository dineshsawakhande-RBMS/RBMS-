"use client";

import { createTheme, alpha, type Theme } from "@mui/material/styles";

/**
 * Custom responsive tiers (kept alongside MUI's defaults so internal components still work):
 *   mobile  = xs       (< 744px)
 *   tablet  = 744px    (744–1111px)
 *   desktop = 1112px   (>= 1112px)
 */
declare module "@mui/material/styles" {
  interface BreakpointOverrides {
    tablet: true;
    desktop: true;
  }
}

const breakpoints = {
  values: { xs: 0, sm: 600, tablet: 744, md: 900, desktop: 1112, lg: 1200, xl: 1536 },
} as const;

const typography = {
  fontFamily: ["var(--font-roboto)", "Roboto", "Segoe UI", "Helvetica", "Arial", "sans-serif"].join(","),
  h1: { fontSize: "1.9rem", fontWeight: 700, letterSpacing: "-0.02em" },
  h2: { fontSize: "1.5rem", fontWeight: 700, letterSpacing: "-0.01em" },
  h3: { fontSize: "1.25rem", fontWeight: 600 },
  h4: { fontSize: "1.6rem", fontWeight: 700, letterSpacing: "-0.01em" },
  h6: { fontWeight: 700 },
  button: { textTransform: "none" as const, fontWeight: 600 },
  subtitle2: { fontWeight: 600 },
} as const;

// Fashion-forward brand: indigo-violet primary with a rose accent.
const BRAND = { primary: "#6C5CE7", primaryDark: "#5848d6", secondary: "#FF5C8A" };

function buildTheme(mode: "light" | "dark"): Theme {
  const isLight = mode === "light";

  const base = createTheme({
    breakpoints,
    shape: { borderRadius: 14 },
    typography,
    palette: {
      mode,
      primary: { main: isLight ? BRAND.primary : "#8B7DFF", dark: BRAND.primaryDark, contrastText: "#fff" },
      secondary: { main: isLight ? BRAND.secondary : "#FF85AC", contrastText: "#fff" },
      success: { main: "#2E9E6B" },
      warning: { main: "#E8A317" },
      error: { main: "#E5484D" },
      info: { main: "#3B82F6" },
      background: isLight
        ? { default: "#F5F6FB", paper: "#FFFFFF" }
        : { default: "#0E1016", paper: "#161A24" },
      divider: isLight ? "rgba(17,24,39,0.08)" : "rgba(255,255,255,0.10)",
    },
  });

  const theme = base;
  return createTheme(base, { components: {
    MuiCssBaseline: {
      styleOverrides: {
        body: { transition: "background-color .3s ease" },
        "*": { scrollbarWidth: "thin" },
        "*::-webkit-scrollbar": { width: 8, height: 8 },
        "*::-webkit-scrollbar-thumb": {
          background: alpha(theme.palette.text.primary, 0.18),
          borderRadius: 8,
        },
      },
    },
    MuiPaper: { styleOverrides: { root: { backgroundImage: "none" } } },
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: 16,
          border: `1px solid ${theme.palette.divider}`,
          boxShadow: isLight
            ? "0 1px 2px rgba(16,24,40,0.04), 0 8px 24px rgba(16,24,40,0.04)"
            : "0 1px 2px rgba(0,0,0,0.4)",
          transition: "transform .2s ease, box-shadow .2s ease",
        },
      },
    },
    MuiButton: {
      defaultProps: { disableElevation: true },
      styleOverrides: {
        root: { borderRadius: 10, paddingInline: 16, transition: "transform .15s ease, box-shadow .2s ease" },
        containedPrimary: {
          background: `linear-gradient(135deg, ${theme.palette.primary.main}, ${theme.palette.secondary.main})`,
          "&:hover": { filter: "brightness(1.05)", boxShadow: `0 6px 18px ${alpha(theme.palette.primary.main, 0.4)}` },
          "&:active": { transform: "translateY(1px)" },
        },
      },
    },
    MuiAppBar: {
      styleOverrides: {
        root: {
          background: isLight ? alpha("#FFFFFF", 0.8) : alpha("#161A24", 0.8),
          backdropFilter: "blur(10px)",
          color: theme.palette.text.primary,
          borderBottom: `1px solid ${theme.palette.divider}`,
          boxShadow: "none",
        },
      },
    },
    MuiDrawer: { styleOverrides: { paper: { border: "none", backgroundImage: "none" } } },
    MuiListItemButton: {
      styleOverrides: {
        root: {
          borderRadius: 10,
          margin: "2px 8px",
          transition: "background-color .15s ease, color .15s ease",
          "&.Mui-selected": {
            background: alpha(theme.palette.primary.main, isLight ? 0.12 : 0.22),
            color: theme.palette.primary.main,
            "& .MuiListItemIcon-root": { color: theme.palette.primary.main },
            "&:hover": { background: alpha(theme.palette.primary.main, isLight ? 0.18 : 0.28) },
          },
        },
      },
    },
    MuiTableRow: {
      styleOverrides: {
        root: { "&:hover": { backgroundColor: alpha(theme.palette.primary.main, 0.04) } },
      },
    },
    MuiTableCell: { styleOverrides: { head: { fontWeight: 700, color: theme.palette.text.secondary } } },
    MuiChip: { styleOverrides: { root: { fontWeight: 600 } } },
  } });
}

export const lightTheme = buildTheme("light");
export const darkTheme = buildTheme("dark");

export type ThemeMode = "light" | "dark";
export const getTheme = (mode: ThemeMode): Theme => (mode === "dark" ? darkTheme : lightTheme);
