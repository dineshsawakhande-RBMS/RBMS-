"use client";

import { createTheme, type Theme } from "@mui/material/styles";

const shared = {
  shape: { borderRadius: 8 },
  typography: {
    fontFamily: [
      "var(--font-roboto)",
      "Roboto",
      "Helvetica",
      "Arial",
      "sans-serif",
    ].join(","),
    h1: { fontSize: "2rem", fontWeight: 600 },
    h2: { fontSize: "1.5rem", fontWeight: 600 },
    h3: { fontSize: "1.25rem", fontWeight: 600 },
  },
} as const;

export const lightTheme: Theme = createTheme({
  ...shared,
  palette: {
    mode: "light",
    primary: { main: "#1565c0" },
    secondary: { main: "#00897b" },
    background: { default: "#f4f6f8", paper: "#ffffff" },
  },
});

export const darkTheme: Theme = createTheme({
  ...shared,
  palette: {
    mode: "dark",
    primary: { main: "#90caf9" },
    secondary: { main: "#4db6ac" },
    background: { default: "#0f1419", paper: "#1a1f26" },
  },
});

export type ThemeMode = "light" | "dark";

export const getTheme = (mode: ThemeMode): Theme =>
  mode === "dark" ? darkTheme : lightTheme;
