# RBMS Production Go-Live Checklist

Work through every section before flipping production traffic on. Each item is
a checkbox; treat unchecked items in **Security**, **Reliability/HA**, and
**Data** as release blockers.

---

## 1. Security

### Application (OWASP Top 10)

- [ ] Input validation on all API endpoints (server-side, not just client).
- [ ] Output encoding / parameterized queries (no string-built SQL) — protects
      against injection.
- [ ] Anti-CSRF where cookie auth is used; CORS locked to known origins.
- [ ] Security headers set (HSTS, `X-Content-Type-Options`, `X-Frame-Options`
      / CSP, `Referrer-Policy`).
- [ ] Dependency scanning (Dependabot / `npm audit` / `dotnet list package
      --vulnerable`) clean of high/critical findings.
- [ ] No secrets, tokens, or connection strings in source, logs, or client
      bundles.

### Authentication & JWT

- [ ] JWT signing key sourced from Secrets Manager (`<prefix>/jwt-signing-key`),
      >= 32 bytes, never committed.
- [ ] Short access-token lifetime; refresh tokens rotated and revocable.
- [ ] Tokens validated for issuer, audience, expiry, and signature on every
      request.
- [ ] Passwords hashed with a modern KDF (bcrypt/Argon2); lockout/backoff on
      repeated failures.

### Authorization (RBAC)

- [ ] Role-based authorization enforced on the **server** for every endpoint
      (Owner / Admin / Manager / Cashier / InventoryClerk / Accountant).
- [ ] Default-deny: new endpoints require an explicit policy.
- [ ] Multi-tenant isolation — every query is scoped by `businessId`.

### Rate limiting & abuse

- [ ] Rate limiting on auth and write-heavy endpoints (app middleware and/or
      ALB/WAF).
- [ ] AWS WAF in front of the ALB (managed rule sets + rate-based rules).

### File handling

- [ ] Uploads (product images, employee/business docs) restricted by type and
      size; content-type sniffed, not trusted.
- [ ] **Virus/malware scanning** on uploaded files before they are served
      (e.g. S3 event → scanning Lambda/ClamAV; quarantine on detection).
- [ ] S3 buckets are private with public access blocked; images served only via
      CloudFront OAC.

### Secrets & transport

- [ ] All secrets in Secrets Manager; rotation plan documented.
- [ ] TLS everywhere — ALB HTTPS listener with a valid ACM cert; `rds.force_ssl`
      enforced; CloudFront `redirect-to-https`.
- [ ] IAM roles least-privilege (task role, execution role, deploy role scoped).

---

## 2. Reliability / High Availability

- [ ] VPC spans 2 AZs; public + private subnets in each; NAT per AZ.
- [ ] RDS **Multi-AZ** enabled (`db_multi_az = true`).
- [ ] ECS service `desired_count` >= 2 spread across AZs.
- [ ] Autoscaling configured (CPU 60% / memory 70% target tracking; min 2,
      max 6) and load-tested.
- [ ] ALB + container **health checks** on `/health`; unhealthy tasks drained
      and replaced.
- [ ] Deployment strategy keeps min healthy at 100% (rolling, no downtime).
- [ ] **Backups:**
  - [ ] RDS automated daily backups, retention >= 14 days.
  - [ ] AWS Backup plan covering RDS (and EBS/EFS if used) with cross-region
        copy for DR.
  - [ ] S3 **versioning** enabled on all buckets (product-images, employee-docs,
        business-docs) — protects against overwrite/delete.
- [ ] RDS deletion protection ON; final snapshot configured.
- [ ] Documented RTO/RPO and a tested restore runbook.

---

## 3. Observability

- [ ] **Structured (JSON) logging** from the API; correlation/request IDs
      propagated.
- [ ] Logs shipped to CloudWatch (`/ecs/<prefix>-api`) with sensible retention
      (>= 30 days) and PII redaction.
- [ ] Container Insights enabled on the ECS cluster.
- [ ] **CloudWatch alarms** with SNS notification for:
  - [ ] ALB 5xx rate and target response time (p95/p99).
  - [ ] ECS CPU/memory saturation and unhealthy task count.
  - [ ] RDS CPU, free storage, free memory, and connection count.
  - [ ] Failed deployments / task crash loops.
- [ ] Dashboards for golden signals (latency, traffic, errors, saturation).
- [ ] Optional: distributed tracing (X-Ray / OpenTelemetry) wired through the
      stack.

---

## 4. Performance

- [ ] Load test at expected peak; autoscaling verified to react.
- [ ] DB indexes for hot query paths; slow-query logging on
      (`log_min_duration_statement = 1000`).
- [ ] Connection pooling sized appropriately for Fargate task count.
- [ ] CloudFront caching tuned for static assets/images; compression on.
- [ ] Frontend: production build, code-splitting, image optimization, and CDN
      for static assets.
- [ ] Pagination on all list endpoints; no unbounded queries.

---

## 5. Data

- [ ] Migrations are versioned, idempotent, and run via a controlled job (not
      at app startup in production) — see deployment guide §7.
- [ ] Seed/reference data (roles, categories, tax rates) applied.
- [ ] **Point-in-time recovery (PITR)** enabled on RDS and recovery tested.
- [ ] Backup restores rehearsed end-to-end at least once.
- [ ] Data retention and archival policy defined.

---

## 6. Compliance

- [ ] **Audit logging** of sensitive actions (logins, role changes, financial
      transactions, data exports) with actor, timestamp, and before/after.
- [ ] **Soft delete** for business records (no hard deletes of transactional
      data); deletes are recoverable and audited.
- [ ] **PII handling for Aadhaar / PAN** and other identifiers:
  - [ ] Encrypted at rest (KMS) and in transit.
  - [ ] Masked in UI and logs; full value access restricted by role and audited.
  - [ ] Collected only where legally required; retention/erasure policy defined.
- [ ] Privacy policy and data-processing terms in place.
- [ ] Access reviews scheduled (who can see PII / financials).

---

## 7. Cost

- [ ] Right-sized Fargate task CPU/memory and RDS instance class.
- [ ] Autoscaling min/max tuned to avoid over-provisioning.
- [ ] CloudWatch log retention bounded (logs are a common cost surprise).
- [ ] S3 lifecycle rules (transition old doc versions to IA/Glacier; expire
      noncurrent versions).
- [ ] AWS Budgets + cost anomaly alerts configured.
- [ ] CloudFront price class matches the audience geography.

---

## 8. Operational readiness

- [ ] Runbooks: deploy, rollback (deployment guide §9), incident response,
      on-call rotation.
- [ ] Staging environment mirrors production and is used as a release gate.
- [ ] CI green (backend + frontend) on `main`; deploy gated by the `production`
      environment.
- [ ] DNS, TLS certs, and CloudFront verified (deployment guide §8).
- [ ] Smoke test passed post-deploy (deployment guide §10).

---

## 9. Sign-off

| Area               | Owner | Status | Date |
| ------------------ | ----- | ------ | ---- |
| Security           |       |        |      |
| Reliability / HA   |       |        |      |
| Observability      |       |        |      |
| Performance        |       |        |      |
| Data               |       |        |      |
| Compliance         |       |        |      |
| Cost               |       |        |      |
| Operational        |       |        |      |

**Release approved by:** ____________________  **Date:** ____________

**Rollback owner on call:** ____________________
