# RBMS — project guide for Claude

Retail Business Management System for a single ladies' western-wear shop (multi-tenant-ready,
designed to scale to multi-store later). Built incrementally; **runs locally, not on AWS**
(the AWS Terraform/CI-CD scaffolding exists but the owner deploys later, if ever).

## Stack
- **Backend:** ASP.NET Core 9 Web API, EF Core 9 (Npgsql/PostgreSQL 16), Clean Architecture,
  CQRS via MediatR, Repository + Unit of Work, FluentValidation, Serilog, JWT auth + RBAC,
  QuestPDF (invoices/slips), ClosedXML (Excel).
- **Frontend:** Next.js 15 (App Router), React 19, TypeScript (strict), MUI v6, TanStack
  Query v5, Zustand, Formik+Yup, Recharts, axios.
- **DB:** PostgreSQL 16 (local, service `postgresql-x64-16`).

## Repo layout
```
backend/   src/{RBMS.Domain,RBMS.Application,RBMS.Infrastructure,RBMS.Api} + tests/{Unit,Integration}
frontend/  Next.js app (src/app, src/features, src/components, src/lib, src/types)
docs/      schema.sql, architecture diagrams, roadmap, deployment guide, production checklist
infra/     terraform + docker (for future AWS deploy)
scripts/   reset-demo-data.sql
```

## Architecture conventions (match these)
- **Clean Architecture**: Domain (no deps) ← Application (CQRS, interfaces) ← Infrastructure/Api.
- **Adding a module**: Domain entity (derive `AuditableEntity` for tenant+audit+soft-delete) →
  `IEntityTypeConfiguration` in Infrastructure/Persistence/Configurations → add `DbSet` to
  `IApplicationDbContext` + `ApplicationDbContext` + a global query filter → CQRS under
  `Application/Features/<Module>` (commands marked `ITransactionalRequest`, FluentValidation
  validators) → controller extending `ApiControllerBase` with `[HasPermission(...)]` →
  `dotnet ef migrations add` → tests.
- **Cross-cutting (automatic):** multi-tenant global query filter (`TenantId == TenantId`),
  soft delete (`IsDeleted` filter; `Remove()` → flagged, never physically deleted), audit-log
  SaveChanges interceptor, `xmin` optimistic concurrency, validation/logging/transaction
  pipeline behaviors.
- **Stock is never mutated directly** — every change goes through `IStockLedger.ApplyAsync`
  (append-only `StockMovement` + projected `Inventory`, moving-avg cost, no-negative guard).
  Purchases (`PurchaseIn`), Sales (`SaleOut`), returns, adjustments all feed it.
- **Transactions:** `IUnitOfWork.ExecuteInTransactionAsync` wraps work in the EF **execution
  strategy** (required because retry-on-failure is enabled). Never call `BeginTransaction`
  directly.
- **Enums** serialize as strings (global `JsonStringEnumConverter`). Integration tests that
  deserialize enum-bearing DTOs use `TestJson.Options`.
- **Responsive tiers** (custom MUI breakpoints kept alongside defaults): `xs`=mobile (<744),
  `tablet` (744–1111), `desktop` (≥1112). Use these keys in `sx`/`Grid size`.

## Run locally
- **DB connection** is in **.NET user-secrets** (`ConnectionStrings:Default`, already set on
  this machine) — do NOT hardcode the password anywhere committed.
- Backend: `cd backend && dotnet run --project src/RBMS.Api --urls http://localhost:5080`
  (Development auto-migrates + seeds demo data).
- Frontend: `cd frontend && npm run dev` (Next picks 3000 or 3001; CORS allows any localhost in Dev).
- **Login:** `owner` / `Password123!` (also `cashier`). Seeded store id
  `aaaaaaaa-0000-0000-0000-000000000002`. Frontend API base set in `frontend/.env.local`.

## Dev workflow gotchas (IMPORTANT — learned the hard way)
1. **Building/migrating the backend requires the running API to be stopped** (file locks).
   Ask the user to Ctrl+C the `:5080` terminal first.
2. **After `dotnet ef migrations add`, REBUILD before `dotnet run --no-build`** — the migration
   files are written *after* the tool's build, so a stale snapshot in `bin` causes
   `PendingModelChangesWarning` at startup. Always `dotnet build` after adding a migration.
3. **Verify each feature on PostgreSQL**, not just InMemory tests — InMemory translates any
   LINQ, so it misses Npgsql-specific issues (group-by translation, etc.). Run a throwaway
   instance on **port 5081** (so it doesn't clash with the user's :5080) and curl the endpoints.
4. **Reset demo data** between verifications with `scripts/reset-demo-data.sql` (verification
   creates test rows).
5. **Git:** a push token is stored in the local `.git/config` remote, so `git push origin main`
   works with no prompt. When the user says **"commit"**, commit AND push. End commit messages
   with the Co-Authored-By line. (Token expires ~monthly; then the user supplies one fresh PAT.)
6. **PowerShell quirks:** `$pid` is reserved (use another name); Windows PowerShell 5.1 (no
   ternary/`&&`); multipart upload via `curl.exe -F`.
7. The owner is on Windows; commits are authored as `dineshsawakhande-RBMS` (repo-local git
   identity), NOT the global Trepup identity.

## Test / build
- `cd backend && dotnet test RBMS.sln` — currently **94 tests** (unit + integration) passing.
- `cd frontend && npm run typecheck` — strict, must stay clean.

## Modules status
**Done (Phase 1 + much of Phase 2):** Auth+RBAC, Dashboard (live KPIs), Products (CRUD + image/
video upload via local file store), Inventory (ledger + adjustments), Suppliers (+ ledger),
Purchases (+ returns), Sales/POS (+ returns, customers, loyalty, GST PDF invoice), Customers,
Reports (CSV + Excel), Employees, Salary/payroll (PDF slips), Documents (searchable store +
upload/download via local file store, type filter, tags, expiry alerts), Attendance & Leave
(monthly marking grid, leave approval auto-marks attendance, monthly summary prefills payroll's
working/present days), Notifications (per-user in-app bell — low-stock / doc-expiry / salary-due /
leave-pending, reconciled on refresh; leave-pending routes to holders of the `leave.approve`
permission, who alone may decide leaves). Soft-delete + edit on all master modules. Global toasts.
Responsive shell (sidebar / hamburger / mobile bottom-nav).

**Phase 2 is complete.** (Email/SES delivery of notifications stays deferred — local-only, no AWS.)

**Phase 3 (in progress):**
- **Done:** Analytics — dead/slow-moving stock (store-scoped units-sold vs on-hand, capital
  tied up) and customer retention (repeat rate, new-vs-returning monthly trend, top spenders).
  Read-only, reuses `report.view`; `/api/analytics/dead-stock` + `/customer-retention`.
- **Done:** Multi-store activation — Stores CRUD (`store.view`/`store.manage`), an active-store
  switcher in the app bar (Zustand `storeStore`, persisted; `useEffectiveStoreId()` feeds every
  store-scoped page: inventory/POS/purchases/reports/analytics/dashboard), and inter-store stock
  **transfers** via the ledger (`POST /api/inventory/transfers`, TransferOut+TransferIn sharing a
  reference id, source avg-cost carried over, no-negative guard). No migration (Store table
  pre-existed). Switcher auto-hides for a single store.
- **Next:** WhatsApp, mobile app.

**AWS deploy stays LAST — only once the whole app is done** (owner's call). Everything runs
locally until then; the Terraform/CI-CD scaffolding is untouched.

## Deferred / known
- Aadhaar/PAN full encryption (employees keep only non-sensitive fields + account last-4).
- Email/SES + AWS deploy (intentionally local-only for now).
- Product-list thumbnails (images show in the product editor).
