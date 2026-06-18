# RBMS Backend — ASP.NET Core 9, Clean Architecture, CQRS

## Projects

| Project | Responsibility |
|---|---|
| `src/RBMS.Domain` | Entities, enums, base classes. No external dependencies. |
| `src/RBMS.Application` | CQRS commands/queries (MediatR), DTOs, validators, pipeline behaviors, abstractions. |
| `src/RBMS.Infrastructure` | EF Core (PostgreSQL), repositories + UoW, SaveChanges interceptors, JWT, AWS (S3/SES). |
| `src/RBMS.Api` | Controllers, middleware, JWT auth, permission policies, rate limiting, Swagger, DI root. |
| `tests/RBMS.UnitTests` | Validator + domain/application unit tests. |
| `tests/RBMS.IntegrationTests` | `WebApplicationFactory` end-to-end tests (in-memory DB, seeded auth). |

See [`../docs/architecture/clean-architecture.md`](../docs/architecture/clean-architecture.md).

## What is implemented in the foundation

- **Cross-cutting (every module inherits these):** multi-tenant query filter, soft delete,
  audit logging with old/new values (SaveChanges interceptor), optimistic concurrency via
  `xmin`, validation + logging + transaction pipeline behaviors.
- **Auth vertical:** `POST /api/auth/login`, `POST /api/auth/refresh` — JWT access token +
  rotating refresh token (hashed, reuse-detection), lockout after failed attempts, login
  history.
- **Product module (worked CQRS example):** list/get/create/update/soft-delete with
  validators and permission-gated endpoints.
- **Inventory module:** append-only stock ledger via `IStockLedger` — the single
  choke-point for every stock change. Stock is **never** mutated directly; each change
  appends a `StockMovement` and re-projects the `inventory` row (with moving-average cost
  and a non-negative-stock guard). Endpoints: stock levels, low-stock, movement history,
  manual adjustments, damaged write-off (`/api/inventory/*`).
- **Purchase module:** suppliers (+ account ledger / outstanding balance), goods-receipt
  purchases that feed the stock ledger (`PurchaseIn`, moving-average cost) and post to the
  supplier ledger (credit + payment debit), and purchase returns (`PurchaseReturn` out +
  supplier debit). Endpoints: `/api/suppliers/*`, `/api/purchases/*`. *(Formal PO workflow
  and S3 invoice upload deferred to the next iteration.)*
- **Sales / POS module:** billing that feeds the stock ledger (`SaleOut`, oversell-guarded),
  snapshots COGS (moving-avg cost) for profit, splits GST (CGST/SGST), records payments and
  change due, and processes sale returns (`SaleReturn` back into stock). Endpoints:
  `/api/sales/*`. *(Barcode scan, thermal printing, customer/loyalty deferred.)*
- **Reports module:** Sales, Purchase, Inventory-valuation, and Profit-by-product reports
  over a date range, each with **CSV export** (`/api/reports/{sales|purchases|inventory|profit}?format=csv`).
  *(Excel/PDF export deferred — ClosedXML/QuestPDF next.)*
- **Dashboard:** `GET /api/dashboard/summary` — aggregates real sales/profit/inventory/top
  products in one round-trip.
- **RBAC:** `[HasPermission("...")]` attribute backed by an on-demand policy provider.

## Run locally

```bash
# Requires a PostgreSQL server reachable via appsettings.Development.json.
dotnet restore
dotnet run --project src/RBMS.Api          # https://localhost:xxxx/swagger
```

In the **Development** environment the API automatically applies migrations and seeds demo
data on startup (see `DbSeeder`) — no manual `ef database update` needed. The seed creates
a tenant, a store, the full permission catalogue, roles, two users, and sample products
with opening stock.

**Demo logins** (POST `/api/auth/login`):

| Username | Password | Role | Can do |
|---|---|---|---|
| `owner` | `Password123!` | Owner | everything |
| `cashier` | `Password123!` | Cashier | dashboard, products (view), inventory (view), sales, customers |

The seed is idempotent — it skips if the tenant already exists. To re-seed, drop the
database and restart. Seeding runs **only** in Development; it never runs in tests or
production.

## Migrations

```bash
# Add a migration after changing the model:
dotnet ef migrations add <Name> -p src/RBMS.Infrastructure -s src/RBMS.Api -o Persistence/Migrations
# Apply:
dotnet ef database update -p src/RBMS.Infrastructure -s src/RBMS.Api
```

In production the deploy pipeline applies migrations by running the API with `--migrate`
(`dotnet RBMS.Api.dll --migrate`), then starts the service.

## Tests

```bash
dotnet test            # 15 tests: 10 unit + 5 integration
```

Integration tests swap PostgreSQL for the EF in-memory provider, so they run without Docker.
For CI against real PostgreSQL, point the factory at the `postgres` service container (see
`.github/workflows/backend-ci.yml`) and switch to `Testcontainers.PostgreSql`.

## Configuration (env vars / appsettings)

| Key | Purpose |
|---|---|
| `ConnectionStrings__Default` | PostgreSQL connection string |
| `Jwt__SigningKey` | HMAC signing key (≥32 chars) — from Secrets Manager in prod |
| `Jwt__Issuer`, `Jwt__Audience` | Token issuer/audience |
| `AwsStorage__DocumentsBucket`, `AwsStorage__ImagesBucket` | S3 buckets |
| `Email__FromAddress` | SES sender |
| `Cors__AllowedOrigins__0` | Allowed frontend origin(s) |

## Adding a new module (the pattern)

1. Add entities to `RBMS.Domain` (derive from `AuditableEntity` for tenant+audit+soft-delete).
2. Add an `IEntityTypeConfiguration<T>` in `Infrastructure/Persistence/Configurations`.
3. Expose the `DbSet<T>` on `IApplicationDbContext` + `ApplicationDbContext`; add a query filter.
4. Write commands/queries under `Application/Features/<Module>` with validators; mark commands
   `ITransactionalRequest`.
5. Add a controller deriving from `ApiControllerBase` with `[HasPermission(...)]`.
6. `dotnet ef migrations add ...`. Add tests.
