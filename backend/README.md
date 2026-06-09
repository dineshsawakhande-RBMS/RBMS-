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
- **Dashboard:** `GET /api/dashboard/summary` aggregate query.
- **RBAC:** `[HasPermission("...")]` attribute backed by an on-demand policy provider.

## Run locally

```bash
# Requires a PostgreSQL reachable via appsettings.Development.json (or docker compose up db).
dotnet restore
dotnet ef database update -p src/RBMS.Infrastructure -s src/RBMS.Api   # apply migrations
dotnet run --project src/RBMS.Api                                       # https://localhost:xxxx/swagger
```

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
