# Docker

Container images and local orchestration for RBMS.

## Files

| File             | Purpose                                                       |
| ---------------- | ------------------------------------------------------------- |
| `api.Dockerfile` | Multi-stage ASP.NET Core 9 Web API (`RBMS.Api`), non-root     |
| `web.Dockerfile` | Multi-stage Next.js 15 standalone server, non-root            |

Both Dockerfiles assume the **repo root** as the build context so they can see
`backend/` and `frontend/` respectively.

## Local development

From the repo root:

```bash
cp .env.example .env        # adjust passwords/keys
docker compose up --build
```

Services:

| Service | Port | Notes                                            |
| ------- | ---- | ------------------------------------------------ |
| `db`    | 5432 | postgres:16-alpine, persisted in `db_data` volume, healthchecked |
| `api`   | 8080 | waits for `db` to be healthy; reads connection string + JWT from env |
| `web`   | 3000 | `NEXT_PUBLIC_API_BASE_URL` baked at build time   |

Open http://localhost:3000 for the web app and http://localhost:8080/health for
the API health probe.

## Building images individually

```bash
docker build -f infra/docker/api.Dockerfile -t rbms-api:local .
docker build -f infra/docker/web.Dockerfile \
  --build-arg NEXT_PUBLIC_API_BASE_URL=https://api.example.com \
  -t rbms-web:local .
```

## Notes

- `NEXT_PUBLIC_*` values are inlined into the client bundle **at build time**,
  so the web image must be rebuilt to change the API URL.
- The API image installs `curl` solely for the container/ALB health check.
- Both images run as a dedicated non-root user (uid 1001).
