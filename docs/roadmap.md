# Delivery Roadmap

Build in three phases. **Phase 1 alone runs the entire shop.** Each phase is independently
shippable.

## Phase 1 — Run the business (target ~2–3 months)

The operational core. Everything here directly touches daily money-in / money-out.

| Module | Backend | Frontend | Notes |
|---|---|---|---|
| **Authentication & RBAC** | JWT access + rotating refresh, roles/permissions, login history | Login, refresh flow, route guards | Spine for everything else |
| **Dashboard** | Summary query aggregating sales/purchase/inventory/cash | MUI cards + Recharts | Read-only over other modules |
| **Product** | Product + ProductVariant + images, categories, brands | List/detail/CRUD, variant matrix | ✅ worked example in this repo |
| **Inventory** | Append-only `stock_movements`; projected `inventory` | Stock views, adjustments | Never mutate stock directly |
| **Purchase** | POs, goods receipt, returns; auto stock-in | PO entry, invoice upload | Updates inventory via movements |
| **Sales / POS** | Billing, payments, GST invoice, returns; auto stock-out | POS screen, barcode, thermal print | Updates inventory via movements |
| **Reports** | Sales/Purchase/Inventory/Profit | Tables + PDF/Excel/CSV export | Built on the above |
| **Document Mgmt (early)** | S3 upload, tags, search, expiry | Upload + search UI | Worth building early — one home for GST cert, invoices, rent agreement, etc. |

**Why Document Management is pulled into Phase 1:** even for a single shop, having GST
certificates, supplier invoices, rent agreements, and key documents in one searchable,
backed-up place (instead of WhatsApp / random folders) pays off immediately and reuses the
same S3 + CloudFront plumbing the Product images need.

## Phase 2 — People & paper

| Module | Notes |
|---|---|
| **Employee Management** | Personal/bank details, Aadhaar/PAN (encrypted PII), docs to S3, attendance, leave |
| **Salary Management** | Attendance-based salary, advances, bonus, deductions, PDF salary slips |
| **Document Mgmt (full)** | Versioning, OCR metadata, expiry alerts, full taxonomy |
| **Notifications** | Email via SES: low stock, salary due, document expiry, backup failure |
| **Audit module UI** | Surface the already-captured `audit_logs` for browsing |

## Phase 3 — Grow

| Module | Notes |
|---|---|
| **Analytics** | Slow-moving / dead stock, profit by category, retention, trend lines |
| **Multi-store** | Activate `store_id` everywhere, per-store stock & transfers, store switcher |
| **Mobile app** | React Native / Expo against the same API |
| **WhatsApp integration** | Invoices, low-stock alerts, marketing |
| **Customer loyalty** | Points earn/burn rules, birthday/anniversary campaigns |

## Cross-cutting (built into the foundation from day one)

These are **not** deferred — they are baked into the base classes and infrastructure so
every module inherits them for free:

- Multi-tenant scoping (`tenant_id` + global query filter)
- Soft delete (`is_deleted` + global query filter)
- Audit logging (SaveChanges interceptor, old/new values)
- Activity tracking & login history
- Optimistic concurrency (`xmin` row version)
- Input validation (FluentValidation pipeline behavior)
- Structured logging + correlation IDs

## Sequencing rationale

1. **Auth first** — nothing is secure or attributable without it.
2. **Product → Inventory → Purchase → Sales** — this is the natural data-dependency order:
   you must have products before stock, stock before purchasing/selling.
3. **Reports last in Phase 1** — they only aggregate what the prior modules produce.
4. **Stock ledger discipline** is established before Purchase/Sales so both feed the same
   append-only movements table from the start.
