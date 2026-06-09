# RBMS Frontend

Web client for the Retail Business Management System.

## Stack

- **Next.js 15** (App Router) + **React 19** + **TypeScript** (strict)
- **Material UI v6** (`@mui/material`, `@mui/icons-material`) with Emotion
- **TanStack React Query v5** for server state
- **Zustand** for client/auth state (persisted)
- **Formik + Yup** for forms and validation
- **Recharts** for dashboard charts
- **Axios** for HTTP, with JWT + refresh-token interceptors

## Getting started

```bash
cp .env.example .env.local   # set NEXT_PUBLIC_API_BASE_URL
npm install
npm run dev                  # http://localhost:3000
```

| Script              | Purpose                                  |
| ------------------- | ---------------------------------------- |
| `npm run dev`       | Start the dev server                     |
| `npm run build`     | Production build (standalone output)     |
| `npm run start`     | Serve the production build               |
| `npm run lint`      | ESLint (next + prettier)                 |
| `npm run typecheck` | `tsc --noEmit`                           |
| `npm run format`    | Prettier write                           |

## Project structure

```
src/
  app/
    layout.tsx                  Root layout; wires AppProviders + fonts
    page.tsx                    Redirects "/" -> "/dashboard"
    (auth)/login/page.tsx       Formik + Yup login form (MUI)
    (dashboard)/
      layout.tsx                AppBar + Drawer navigation shell
      dashboard/page.tsx        KPI cards + Recharts charts + low-stock table
  components/
    providers/AppProviders.tsx  Emotion (App Router cache) + Theme + Query + color mode
    dashboard/StatCard.tsx      Reusable KPI card
  features/
    dashboard/
      api.ts                    fetchDashboardSummary + mock fallback
      hooks.ts                  useDashboardSummary (React Query)
  lib/
    apiClient.ts                Axios instance; JWT + 401 refresh retry flow
  store/
    authStore.ts                Zustand auth store (persisted) + role helpers
  theme/
    theme.ts                    MUI light/dark themes
  types/                        Shared types (User, Role, DashboardSummary, ...)
```

## Architecture notes

- **App Router route groups** `(auth)` and `(dashboard)` keep the login screen
  outside the authenticated shell. The dashboard group owns the AppBar/Drawer.
- **Emotion SSR** is handled by `AppRouterCacheProvider` from
  `@mui/material-nextjs/v15-appRouter`, which is the supported integration for
  Next 15's App Router (no manual `useServerInsertedHTML` plumbing needed).
- **Auth** lives in a Zustand store persisted to `localStorage`. The axios
  client reads the access token via a non-reactive `authStore` accessor and
  transparently refreshes on `401` using a single-flight refresh promise.
- **Data fetching** goes through React Query hooks under `features/*`. The
  dashboard hook falls back to mock data when the API is unreachable so the UI
  is demonstrable before the backend is live.

## Environment variables

| Variable                     | Description                          |
| ---------------------------- | ------------------------------------ |
| `NEXT_PUBLIC_API_BASE_URL`   | Backend base URL (no trailing slash) |
| `NEXT_PUBLIC_APP_NAME`       | App display name                     |
| `NEXT_PUBLIC_API_TIMEOUT_MS` | Axios request timeout (ms)           |
