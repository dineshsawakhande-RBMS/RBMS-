# RBMS — Retail Business Management System

A production-oriented, multi-tenant-ready retail management platform built first for a
single **ladies' western wear** store, designed to scale to multi-store operations.

> **Status: Foundation.** This repository currently contains the architecture, the
> canonical database schema, diagrams, a buildable Clean Architecture backend skeleton
> (with cross-cutting concerns and one fully-worked module), a Next.js frontend skeleton,
> and the infra / CI-CD / deployment scaffolding. Business modules are filled in
> incrementally per the [roadmap](docs/roadmap.md).

## Tech stack

| Layer        | Technology |
|--------------|------------|
| Frontend     | Next.js 15 (App Router), React 19, TypeScript, MUI v6, TanStack React Query v5, Zustand, Formik + Yup, Recharts |
| Backend      | ASP.NET Core 9 Web API, EF Core 9, Clean Architecture, CQRS (MediatR), Repository + Unit of Work |
| Database     | PostgreSQL 16 (AWS RDS) |
| Auth         | JWT access token + rotating refresh token, role-based authorization |
| Cloud / Infra| AWS ECS Fargate, RDS, S3, CloudFront, SES, Secrets Manager, CloudWatch, AWS Backup; Terraform; GitHub Actions; Docker |

## Repository layout

```
inventory-sys/
├── backend/                 # ASP.NET Core 9 — Clean Architecture solution
│   ├── src/
│   │   ├── RBMS.Domain/         # Entities, enums, domain events, no dependencies
│   │   ├── RBMS.Application/    # CQRS handlers, DTOs, validators, abstractions (MediatR)
│   │   ├── RBMS.Infrastructure/ # EF Core, repositories, UoW, auth, AWS, interceptors
│   │   └── RBMS.Api/            # Controllers, middleware, DI composition root
│   └── tests/
│       ├── RBMS.UnitTests/
│       └── RBMS.IntegrationTests/
├── frontend/                # Next.js 15 app
├── infra/
│   ├── terraform/           # AWS infrastructure as code
│   └── docker/              # Dockerfiles
├── docs/
│   ├── database/schema.sql      # Canonical PostgreSQL schema (source of truth)
│   ├── architecture/           # ER diagram, AWS + Clean Architecture diagrams
│   ├── roadmap.md
│   ├── deployment-guide.md
│   └── production-checklist.md
├── .github/workflows/       # CI/CD
└── docker-compose.yml       # Local dev: postgres + api + web
```

## Architecture principles

- **Clean Architecture** — dependencies point inward; Domain has zero framework deps.
- **CQRS via MediatR** — commands and queries are separate; cross-cutting concerns
  (validation, logging, transactions, auth) live in pipeline behaviors.
- **Multi-tenant ready** — every business table carries `tenant_id`; a global query
  filter scopes all reads automatically. `store_id` enables future multi-store.
- **Soft delete** — rows are flagged `is_deleted`, never physically removed; a global
  query filter hides them. Hard delete is an explicit, audited, privileged action.
- **Append-only stock ledger** — inventory is **never** mutated directly. All changes go
  through `stock_movements`; current stock is a projection kept in sync transactionally.
- **Full audit + activity tracking** — an EF Core SaveChanges interceptor records who
  changed what (old/new values) into `audit_logs`; logins and significant actions land in
  `activity_logs` / `login_history`.

See [`docs/architecture/`](docs/architecture/) for diagrams and the deeper design notes.

## Getting started (local)

```bash
# 1. Database + API + Web via Docker (once Docker is installed)
docker compose up -d

# 2. Or run pieces directly:
# Backend
cd backend && dotnet restore && dotnet run --project src/RBMS.Api
# Frontend
cd frontend && npm install && npm run dev
```

See [docs/deployment-guide.md](docs/deployment-guide.md) for AWS deployment and
[docs/production-checklist.md](docs/production-checklist.md) before going live.

## Roadmap

Delivery is phased — see [docs/roadmap.md](docs/roadmap.md). Phase 1 (Auth, Dashboard,
Product, Inventory, Purchase, Sales, Reports, plus early Document Management) is enough to
run the shop end-to-end.
