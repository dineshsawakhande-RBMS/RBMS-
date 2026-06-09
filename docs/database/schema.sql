-- =============================================================================
-- RBMS — Retail Business Management System
-- Canonical PostgreSQL 16 schema (source of truth)
-- =============================================================================
-- Conventions
--   * UUID primary keys (gen_random_uuid()) — distribution / multi-tenant friendly.
--   * Every business table carries: tenant_id, store_id (nullable until multi-store),
--     audit columns (created_at/by, updated_at/by) and soft-delete columns
--     (is_deleted, deleted_at, deleted_by).
--   * Money: NUMERIC(14,2). Quantities: NUMERIC(14,3). Percentages: NUMERIC(5,2).
--   * Inventory is NEVER updated directly — see stock_movements (append-only ledger)
--     and the projected `inventory` table.
--   * Optimistic concurrency uses the system column xmin (mapped in EF as row version);
--     no explicit version column is required.
--   * Enumerations use native PostgreSQL enum types for stable, small, closed sets.
-- =============================================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;   -- gen_random_uuid(), digest()
CREATE EXTENSION IF NOT EXISTS citext;     -- case-insensitive emails / codes
CREATE EXTENSION IF NOT EXISTS pg_trgm;    -- trigram search for documents/products

-- =============================================================================
-- ENUM TYPES
-- =============================================================================
CREATE TYPE gender                AS ENUM ('Male', 'Female', 'Other');
CREATE TYPE employment_status     AS ENUM ('Active', 'OnLeave', 'Suspended', 'Resigned', 'Terminated');
CREATE TYPE attendance_status     AS ENUM ('Present', 'Absent', 'HalfDay', 'Leave', 'Holiday', 'WeekOff');
CREATE TYPE leave_status          AS ENUM ('Pending', 'Approved', 'Rejected', 'Cancelled');
CREATE TYPE salary_component_kind AS ENUM ('Earning', 'Deduction');
CREATE TYPE payroll_status        AS ENUM ('Draft', 'Generated', 'Approved', 'Paid');
CREATE TYPE stock_movement_type   AS ENUM ('PurchaseIn','SaleOut','PurchaseReturn','SaleReturn',
                                            'TransferIn','TransferOut','Damaged','AdjustmentIn',
                                            'AdjustmentOut','OpeningStock');
CREATE TYPE purchase_order_status AS ENUM ('Draft','Sent','PartiallyReceived','Received','Cancelled');
CREATE TYPE purchase_status       AS ENUM ('Draft','Confirmed','Cancelled');
CREATE TYPE sale_status           AS ENUM ('Draft','Completed','Refunded','PartiallyRefunded','Cancelled');
CREATE TYPE payment_method        AS ENUM ('Cash','Card','UPI','BankTransfer','Wallet','StoreCredit','Cheque');
CREATE TYPE payment_status        AS ENUM ('Pending','Paid','PartiallyPaid','Failed','Refunded');
CREATE TYPE document_owner_type   AS ENUM ('Business','Employee','Supplier','Customer','Product');
CREATE TYPE notification_type     AS ENUM ('LowStock','SalaryDue','DocumentExpiry','BackupFailure','Custom');
CREATE TYPE notification_status   AS ENUM ('Queued','Sent','Failed');
CREATE TYPE loyalty_txn_type      AS ENUM ('Earn','Redeem','Expire','Adjust');
CREATE TYPE audit_action          AS ENUM ('Create','Update','Delete','SoftDelete','Restore','Login','Logout');

-- =============================================================================
-- 1. TENANCY / ORGANISATION
-- =============================================================================
CREATE TABLE tenants (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            TEXT        NOT NULL,
    legal_name      TEXT,
    gstin           VARCHAR(15),
    pan             VARCHAR(10),
    email           CITEXT,
    phone           VARCHAR(20),
    address_line1   TEXT,
    address_line2   TEXT,
    city            TEXT,
    state           TEXT,
    pincode         VARCHAR(10),
    country         TEXT        NOT NULL DEFAULT 'India',
    currency        VARCHAR(3)  NOT NULL DEFAULT 'INR',
    timezone        TEXT        NOT NULL DEFAULT 'Asia/Kolkata',
    is_active       BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    updated_at      TIMESTAMPTZ,
    updated_by      UUID,
    is_deleted      BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at      TIMESTAMPTZ,
    deleted_by      UUID
);

CREATE TABLE stores (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    code            VARCHAR(20) NOT NULL,
    name            TEXT        NOT NULL,
    gstin           VARCHAR(15),
    phone           VARCHAR(20),
    email           CITEXT,
    address_line1   TEXT,
    address_line2   TEXT,
    city            TEXT,
    state           TEXT,
    pincode         VARCHAR(10),
    is_active       BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    updated_at      TIMESTAMPTZ,
    updated_by      UUID,
    is_deleted      BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at      TIMESTAMPTZ,
    deleted_by      UUID,
    UNIQUE (tenant_id, code)
);

-- =============================================================================
-- 2. USERS, ROLES & SECURITY
-- =============================================================================
CREATE TABLE roles (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        REFERENCES tenants(id),          -- NULL => system role
    name            VARCHAR(50) NOT NULL,                        -- SuperAdmin, Owner, Manager, Cashier, InventoryStaff, Accountant
    description     TEXT,
    is_system       BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (tenant_id, name)
);

CREATE TABLE permissions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code            VARCHAR(100) NOT NULL UNIQUE,                -- e.g. "product.create", "sale.refund"
    description     TEXT
);

CREATE TABLE role_permissions (
    role_id         UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    permission_id   UUID NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
    PRIMARY KEY (role_id, permission_id)
);

CREATE TABLE users (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           UUID        NOT NULL REFERENCES tenants(id),
    store_id            UUID        REFERENCES stores(id),       -- home store (nullable)
    username            CITEXT      NOT NULL,
    email               CITEXT      NOT NULL,
    phone               VARCHAR(20),
    full_name           TEXT        NOT NULL,
    password_hash       TEXT        NOT NULL,                    -- ASP.NET Core PasswordHasher
    security_stamp      UUID        NOT NULL DEFAULT gen_random_uuid(),
    is_active           BOOLEAN     NOT NULL DEFAULT TRUE,
    email_confirmed     BOOLEAN     NOT NULL DEFAULT FALSE,
    failed_login_count  INT         NOT NULL DEFAULT 0,
    lockout_end         TIMESTAMPTZ,
    last_login_at       TIMESTAMPTZ,
    must_change_password BOOLEAN    NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by          UUID,
    updated_at          TIMESTAMPTZ,
    updated_by          UUID,
    is_deleted          BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at          TIMESTAMPTZ,
    deleted_by          UUID,
    UNIQUE (tenant_id, username),
    UNIQUE (tenant_id, email)
);

CREATE TABLE user_roles (
    user_id     UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id     UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, role_id)
);

CREATE TABLE refresh_tokens (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash      TEXT        NOT NULL,                        -- SHA-256 of opaque token; raw token never stored
    jti             UUID        NOT NULL,                        -- links to issued access token
    expires_at      TIMESTAMPTZ NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by_ip   INET,
    revoked_at      TIMESTAMPTZ,
    revoked_by_ip   INET,
    replaced_by_id  UUID        REFERENCES refresh_tokens(id),   -- rotation chain
    reason_revoked  TEXT
);
CREATE INDEX ix_refresh_tokens_user      ON refresh_tokens(user_id);
CREATE INDEX ix_refresh_tokens_tokenhash ON refresh_tokens(token_hash);

CREATE TABLE login_history (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    user_id         UUID        REFERENCES users(id),
    username_tried  TEXT,
    succeeded       BOOLEAN     NOT NULL,
    failure_reason  TEXT,
    ip_address      INET,
    user_agent      TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX ix_login_history_user ON login_history(user_id, created_at DESC);

-- =============================================================================
-- 3. AUDIT & ACTIVITY (cross-cutting)
-- =============================================================================
CREATE TABLE audit_logs (
    id              BIGSERIAL PRIMARY KEY,
    tenant_id       UUID,
    user_id         UUID,
    action          audit_action NOT NULL,
    entity_name     TEXT        NOT NULL,
    entity_id       TEXT        NOT NULL,
    old_values      JSONB,
    new_values      JSONB,
    changed_columns TEXT[],
    ip_address      INET,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX ix_audit_logs_entity ON audit_logs(entity_name, entity_id);
CREATE INDEX ix_audit_logs_tenant ON audit_logs(tenant_id, created_at DESC);

CREATE TABLE activity_logs (
    id              BIGSERIAL PRIMARY KEY,
    tenant_id       UUID,
    user_id         UUID,
    activity        TEXT        NOT NULL,        -- human-readable, e.g. "Generated payroll for May 2026"
    category        TEXT,                        -- module name
    metadata        JSONB,
    ip_address      INET,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX ix_activity_logs_tenant ON activity_logs(tenant_id, created_at DESC);

-- =============================================================================
-- 4. EMPLOYEES
-- =============================================================================
CREATE TABLE employees (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           UUID        NOT NULL REFERENCES tenants(id),
    store_id            UUID        REFERENCES stores(id),
    user_id             UUID        REFERENCES users(id),        -- optional link to a login account
    employee_code       VARCHAR(20) NOT NULL,
    full_name           TEXT        NOT NULL,
    gender              gender,
    date_of_birth       DATE,
    mobile              VARCHAR(20) NOT NULL,
    alt_mobile          VARCHAR(20),
    email               CITEXT,
    address_line1       TEXT,
    address_line2       TEXT,
    city                TEXT,
    state               TEXT,
    pincode             VARCHAR(10),
    emergency_contact_name  TEXT,
    emergency_contact_phone VARCHAR(20),
    -- PII: stored encrypted at the application layer (column holds ciphertext)
    aadhaar_encrypted   BYTEA,
    aadhaar_last4       CHAR(4),
    pan_encrypted       BYTEA,
    designation         TEXT,
    department          TEXT,
    joining_date        DATE        NOT NULL,
    exit_date           DATE,
    status              employment_status NOT NULL DEFAULT 'Active',
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by          UUID,
    updated_at          TIMESTAMPTZ,
    updated_by          UUID,
    is_deleted          BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at          TIMESTAMPTZ,
    deleted_by          UUID,
    UNIQUE (tenant_id, employee_code)
);

CREATE TABLE employee_bank_details (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id     UUID        NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    account_holder  TEXT        NOT NULL,
    bank_name       TEXT        NOT NULL,
    branch          TEXT,
    account_number_encrypted BYTEA NOT NULL,
    account_last4   CHAR(4),
    ifsc            VARCHAR(11) NOT NULL,
    upi_id          TEXT,
    is_primary      BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE salary_structures (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    employee_id     UUID        NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    effective_from  DATE        NOT NULL,
    effective_to    DATE,
    ctc_monthly     NUMERIC(14,2) NOT NULL,
    is_active       BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID
);

CREATE TABLE salary_structure_components (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    salary_structure_id UUID NOT NULL REFERENCES salary_structures(id) ON DELETE CASCADE,
    name                TEXT NOT NULL,                  -- Basic, HRA, PF, etc.
    kind                salary_component_kind NOT NULL,
    is_percentage       BOOLEAN NOT NULL DEFAULT FALSE,
    amount              NUMERIC(14,2),                  -- fixed amount, OR
    percentage          NUMERIC(5,2)                    -- percentage of CTC/basic
);

CREATE TABLE attendance (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    store_id        UUID        REFERENCES stores(id),
    employee_id     UUID        NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    work_date       DATE        NOT NULL,
    status          attendance_status NOT NULL,
    check_in        TIME,
    check_out       TIME,
    worked_hours    NUMERIC(5,2),
    remarks         TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    UNIQUE (employee_id, work_date)
);
CREATE INDEX ix_attendance_emp_date ON attendance(employee_id, work_date);

CREATE TABLE leaves (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    employee_id     UUID        NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    leave_type      TEXT        NOT NULL,            -- Casual, Sick, Paid, Unpaid
    from_date       DATE        NOT NULL,
    to_date         DATE        NOT NULL,
    days            NUMERIC(4,1) NOT NULL,
    reason          TEXT,
    status          leave_status NOT NULL DEFAULT 'Pending',
    approved_by     UUID        REFERENCES users(id),
    approved_at     TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE salary_advances (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    employee_id     UUID        NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    amount          NUMERIC(14,2) NOT NULL,
    advance_date    DATE        NOT NULL,
    recovered       NUMERIC(14,2) NOT NULL DEFAULT 0,
    notes           TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID
);

-- Payroll run (one per employee per period)
CREATE TABLE payrolls (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    employee_id     UUID        NOT NULL REFERENCES employees(id),
    period_year     INT         NOT NULL,
    period_month    INT         NOT NULL,            -- 1..12
    working_days    NUMERIC(4,1) NOT NULL,
    present_days    NUMERIC(4,1) NOT NULL,
    gross_earnings  NUMERIC(14,2) NOT NULL,
    total_deductions NUMERIC(14,2) NOT NULL,
    bonus           NUMERIC(14,2) NOT NULL DEFAULT 0,
    advance_deducted NUMERIC(14,2) NOT NULL DEFAULT 0,
    net_pay         NUMERIC(14,2) NOT NULL,
    status          payroll_status NOT NULL DEFAULT 'Draft',
    slip_document_id UUID,                            -- FK added after documents table
    paid_at         TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    UNIQUE (employee_id, period_year, period_month)
);

CREATE TABLE payroll_lines (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    payroll_id      UUID NOT NULL REFERENCES payrolls(id) ON DELETE CASCADE,
    name            TEXT NOT NULL,
    kind            salary_component_kind NOT NULL,
    amount          NUMERIC(14,2) NOT NULL
);

-- =============================================================================
-- 5. SUPPLIERS
-- =============================================================================
CREATE TABLE suppliers (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    code            VARCHAR(20) NOT NULL,
    name            TEXT        NOT NULL,
    gstin           VARCHAR(15),
    contact_person  TEXT,
    phone           VARCHAR(20),
    email           CITEXT,
    address_line1   TEXT,
    address_line2   TEXT,
    city            TEXT,
    state           TEXT,
    pincode         VARCHAR(10),
    payment_terms_days INT      NOT NULL DEFAULT 0,
    opening_balance NUMERIC(14,2) NOT NULL DEFAULT 0,  -- positive => we owe supplier
    is_active       BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    updated_at      TIMESTAMPTZ,
    updated_by      UUID,
    is_deleted      BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at      TIMESTAMPTZ,
    deleted_by      UUID,
    UNIQUE (tenant_id, code)
);

-- Supplier ledger: outstanding balance is the running sum of these entries.
CREATE TABLE supplier_ledger (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    supplier_id     UUID        NOT NULL REFERENCES suppliers(id),
    entry_date      DATE        NOT NULL,
    reference_type  TEXT        NOT NULL,        -- 'Purchase','PurchaseReturn','Payment','Opening','Adjustment'
    reference_id    UUID,
    debit           NUMERIC(14,2) NOT NULL DEFAULT 0,   -- payment to supplier
    credit          NUMERIC(14,2) NOT NULL DEFAULT 0,   -- purchase increases what we owe
    notes           TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID
);
CREATE INDEX ix_supplier_ledger_supplier ON supplier_ledger(supplier_id, entry_date);

-- =============================================================================
-- 6. CUSTOMERS
-- =============================================================================
CREATE TABLE customers (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    name            TEXT        NOT NULL,
    mobile          VARCHAR(20) NOT NULL,
    email           CITEXT,
    address_line1   TEXT,
    address_line2   TEXT,
    city            TEXT,
    state           TEXT,
    pincode         VARCHAR(10),
    birthday        DATE,
    anniversary     DATE,
    loyalty_points  INT         NOT NULL DEFAULT 0,
    is_active       BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    updated_at      TIMESTAMPTZ,
    updated_by      UUID,
    is_deleted      BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at      TIMESTAMPTZ,
    deleted_by      UUID,
    UNIQUE (tenant_id, mobile)
);

CREATE TABLE loyalty_transactions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    customer_id     UUID        NOT NULL REFERENCES customers(id) ON DELETE CASCADE,
    txn_type        loyalty_txn_type NOT NULL,
    points          INT         NOT NULL,        -- signed: +earn / -redeem
    reference_type  TEXT,                         -- 'Sale', etc.
    reference_id    UUID,
    notes           TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- =============================================================================
-- 7. PRODUCTS
-- =============================================================================
CREATE TABLE categories (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    parent_id       UUID        REFERENCES categories(id),
    name            TEXT        NOT NULL,
    description     TEXT,
    is_active       BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    is_deleted      BOOLEAN     NOT NULL DEFAULT FALSE,
    UNIQUE (tenant_id, parent_id, name)
);

CREATE TABLE brands (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    name            TEXT        NOT NULL,
    is_active       BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    is_deleted      BOOLEAN     NOT NULL DEFAULT FALSE,
    UNIQUE (tenant_id, name)
);

CREATE TABLE products (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    category_id     UUID        REFERENCES categories(id),
    brand_id        UUID        REFERENCES brands(id),
    name            TEXT        NOT NULL,
    description     TEXT,
    hsn_code        VARCHAR(10),                 -- HSN for GST
    gst_rate        NUMERIC(5,2) NOT NULL DEFAULT 0,  -- e.g. 5.00, 12.00
    is_active       BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    updated_at      TIMESTAMPTZ,
    updated_by      UUID,
    is_deleted      BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at      TIMESTAMPTZ,
    deleted_by      UUID
);
CREATE INDEX ix_products_tenant_name ON products USING gin (name gin_trgm_ops);

CREATE TABLE product_variants (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    product_id      UUID        NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    sku             VARCHAR(50) NOT NULL,
    barcode         VARCHAR(64),
    size            VARCHAR(30),                 -- XS, S, M, L, XL, 28, 30...
    color           VARCHAR(40),
    purchase_price  NUMERIC(14,2) NOT NULL DEFAULT 0,
    selling_price   NUMERIC(14,2) NOT NULL DEFAULT 0,
    mrp             NUMERIC(14,2),
    reorder_level   NUMERIC(14,3) NOT NULL DEFAULT 0,
    is_active       BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    updated_at      TIMESTAMPTZ,
    updated_by      UUID,
    is_deleted      BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at      TIMESTAMPTZ,
    deleted_by      UUID,
    UNIQUE (tenant_id, sku)
);
CREATE UNIQUE INDEX ux_variant_barcode ON product_variants(tenant_id, barcode) WHERE barcode IS NOT NULL;

CREATE TABLE product_images (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    product_id      UUID        NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    variant_id      UUID        REFERENCES product_variants(id) ON DELETE CASCADE,
    s3_key          TEXT        NOT NULL,        -- object key; served via CloudFront
    alt_text        TEXT,
    sort_order      INT         NOT NULL DEFAULT 0,
    is_primary      BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- =============================================================================
-- 8. INVENTORY  (append-only ledger + projected current stock)
-- =============================================================================
-- Projected current stock per variant per store. Updated transactionally whenever
-- a stock_movements row is written — NEVER set directly by business code.
CREATE TABLE inventory (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    store_id        UUID        NOT NULL REFERENCES stores(id),
    variant_id      UUID        NOT NULL REFERENCES product_variants(id) ON DELETE CASCADE,
    quantity_on_hand NUMERIC(14,3) NOT NULL DEFAULT 0,
    avg_cost        NUMERIC(14,2) NOT NULL DEFAULT 0,   -- moving average cost
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (store_id, variant_id),
    CONSTRAINT ck_inventory_nonneg CHECK (quantity_on_hand >= 0)
);

-- The append-only source of truth for every stock change.
CREATE TABLE stock_movements (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    store_id        UUID        NOT NULL REFERENCES stores(id),
    variant_id      UUID        NOT NULL REFERENCES product_variants(id),
    movement_type   stock_movement_type NOT NULL,
    quantity        NUMERIC(14,3) NOT NULL,      -- signed: +in / -out
    unit_cost       NUMERIC(14,2),               -- cost at movement time (for valuation)
    balance_after   NUMERIC(14,3) NOT NULL,      -- snapshot of on-hand after this movement
    reference_type  TEXT,                         -- 'Purchase','Sale','Transfer','Adjustment','Return'
    reference_id    UUID,
    notes           TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID
);
CREATE INDEX ix_stock_movements_variant ON stock_movements(variant_id, created_at);
CREATE INDEX ix_stock_movements_ref      ON stock_movements(reference_type, reference_id);

-- Stock adjustments / damaged stock header (lines emit stock_movements)
CREATE TABLE stock_adjustments (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    store_id        UUID        NOT NULL REFERENCES stores(id),
    adjustment_no   VARCHAR(30) NOT NULL,
    reason          TEXT        NOT NULL,         -- 'Damaged','Count correction','Theft'
    adjustment_date DATE        NOT NULL DEFAULT CURRENT_DATE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    UNIQUE (tenant_id, adjustment_no)
);

CREATE TABLE stock_adjustment_lines (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    adjustment_id   UUID NOT NULL REFERENCES stock_adjustments(id) ON DELETE CASCADE,
    variant_id      UUID NOT NULL REFERENCES product_variants(id),
    quantity_delta  NUMERIC(14,3) NOT NULL,       -- signed
    unit_cost       NUMERIC(14,2)
);

-- Stock transfer between stores (future multi-store; emits TransferOut/TransferIn)
CREATE TABLE stock_transfers (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    transfer_no     VARCHAR(30) NOT NULL,
    from_store_id   UUID        NOT NULL REFERENCES stores(id),
    to_store_id     UUID        NOT NULL REFERENCES stores(id),
    transfer_date   DATE        NOT NULL DEFAULT CURRENT_DATE,
    status          TEXT        NOT NULL DEFAULT 'Draft',  -- Draft,Dispatched,Received,Cancelled
    notes           TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    UNIQUE (tenant_id, transfer_no)
);

CREATE TABLE stock_transfer_lines (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    transfer_id     UUID NOT NULL REFERENCES stock_transfers(id) ON DELETE CASCADE,
    variant_id      UUID NOT NULL REFERENCES product_variants(id),
    quantity        NUMERIC(14,3) NOT NULL
);

-- =============================================================================
-- 9. PURCHASE
-- =============================================================================
CREATE TABLE purchase_orders (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    store_id        UUID        REFERENCES stores(id),
    supplier_id     UUID        NOT NULL REFERENCES suppliers(id),
    po_number       VARCHAR(30) NOT NULL,
    po_date         DATE        NOT NULL DEFAULT CURRENT_DATE,
    expected_date   DATE,
    status          purchase_order_status NOT NULL DEFAULT 'Draft',
    subtotal        NUMERIC(14,2) NOT NULL DEFAULT 0,
    tax_total       NUMERIC(14,2) NOT NULL DEFAULT 0,
    grand_total     NUMERIC(14,2) NOT NULL DEFAULT 0,
    notes           TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    is_deleted      BOOLEAN     NOT NULL DEFAULT FALSE,
    UNIQUE (tenant_id, po_number)
);

CREATE TABLE purchase_order_items (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    purchase_order_id UUID NOT NULL REFERENCES purchase_orders(id) ON DELETE CASCADE,
    variant_id      UUID NOT NULL REFERENCES product_variants(id),
    quantity        NUMERIC(14,3) NOT NULL,
    received_qty    NUMERIC(14,3) NOT NULL DEFAULT 0,
    unit_price      NUMERIC(14,2) NOT NULL,
    gst_rate        NUMERIC(5,2) NOT NULL DEFAULT 0,
    line_total      NUMERIC(14,2) NOT NULL
);

-- Goods receipt / purchase entry (this is what actually moves stock IN)
CREATE TABLE purchases (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    store_id        UUID        NOT NULL REFERENCES stores(id),
    supplier_id     UUID        NOT NULL REFERENCES suppliers(id),
    purchase_order_id UUID      REFERENCES purchase_orders(id),
    invoice_number  VARCHAR(50),
    invoice_date    DATE        NOT NULL DEFAULT CURRENT_DATE,
    invoice_document_id UUID,                   -- FK added after documents table
    status          purchase_status NOT NULL DEFAULT 'Confirmed',
    subtotal        NUMERIC(14,2) NOT NULL DEFAULT 0,
    discount        NUMERIC(14,2) NOT NULL DEFAULT 0,
    tax_total       NUMERIC(14,2) NOT NULL DEFAULT 0,
    grand_total     NUMERIC(14,2) NOT NULL DEFAULT 0,
    amount_paid     NUMERIC(14,2) NOT NULL DEFAULT 0,
    payment_status  payment_status NOT NULL DEFAULT 'Pending',
    notes           TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    is_deleted      BOOLEAN     NOT NULL DEFAULT FALSE
);
CREATE INDEX ix_purchases_supplier ON purchases(supplier_id, invoice_date);

CREATE TABLE purchase_items (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    purchase_id     UUID NOT NULL REFERENCES purchases(id) ON DELETE CASCADE,
    variant_id      UUID NOT NULL REFERENCES product_variants(id),
    quantity        NUMERIC(14,3) NOT NULL,
    unit_cost       NUMERIC(14,2) NOT NULL,
    gst_rate        NUMERIC(5,2) NOT NULL DEFAULT 0,
    line_total      NUMERIC(14,2) NOT NULL
);

CREATE TABLE purchase_returns (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    store_id        UUID        NOT NULL REFERENCES stores(id),
    supplier_id     UUID        NOT NULL REFERENCES suppliers(id),
    purchase_id     UUID        REFERENCES purchases(id),
    return_number   VARCHAR(30) NOT NULL,
    return_date     DATE        NOT NULL DEFAULT CURRENT_DATE,
    reason          TEXT,
    total_amount    NUMERIC(14,2) NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    UNIQUE (tenant_id, return_number)
);

CREATE TABLE purchase_return_items (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    purchase_return_id UUID NOT NULL REFERENCES purchase_returns(id) ON DELETE CASCADE,
    variant_id      UUID NOT NULL REFERENCES product_variants(id),
    quantity        NUMERIC(14,3) NOT NULL,
    unit_cost       NUMERIC(14,2) NOT NULL,
    line_total      NUMERIC(14,2) NOT NULL
);

-- =============================================================================
-- 10. SALES / POS
-- =============================================================================
CREATE TABLE sales (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    store_id        UUID        NOT NULL REFERENCES stores(id),
    customer_id     UUID        REFERENCES customers(id),
    cashier_id      UUID        REFERENCES users(id),
    invoice_number  VARCHAR(30) NOT NULL,
    invoice_date    TIMESTAMPTZ NOT NULL DEFAULT now(),
    status          sale_status NOT NULL DEFAULT 'Completed',
    subtotal        NUMERIC(14,2) NOT NULL DEFAULT 0,
    discount        NUMERIC(14,2) NOT NULL DEFAULT 0,
    discount_is_pct BOOLEAN     NOT NULL DEFAULT FALSE,
    taxable_amount  NUMERIC(14,2) NOT NULL DEFAULT 0,
    cgst            NUMERIC(14,2) NOT NULL DEFAULT 0,
    sgst            NUMERIC(14,2) NOT NULL DEFAULT 0,
    igst            NUMERIC(14,2) NOT NULL DEFAULT 0,
    round_off       NUMERIC(14,2) NOT NULL DEFAULT 0,
    grand_total     NUMERIC(14,2) NOT NULL DEFAULT 0,
    amount_paid     NUMERIC(14,2) NOT NULL DEFAULT 0,
    change_due      NUMERIC(14,2) NOT NULL DEFAULT 0,
    payment_status  payment_status NOT NULL DEFAULT 'Paid',
    loyalty_earned  INT         NOT NULL DEFAULT 0,
    loyalty_redeemed INT        NOT NULL DEFAULT 0,
    notes           TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    is_deleted      BOOLEAN     NOT NULL DEFAULT FALSE,
    UNIQUE (tenant_id, invoice_number)
);
CREATE INDEX ix_sales_store_date ON sales(store_id, invoice_date);
CREATE INDEX ix_sales_customer   ON sales(customer_id);

CREATE TABLE sale_items (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sale_id         UUID NOT NULL REFERENCES sales(id) ON DELETE CASCADE,
    variant_id      UUID NOT NULL REFERENCES product_variants(id),
    quantity        NUMERIC(14,3) NOT NULL,
    unit_price      NUMERIC(14,2) NOT NULL,      -- selling price at time of sale
    unit_cost       NUMERIC(14,2) NOT NULL DEFAULT 0,  -- COGS snapshot for profit reports
    discount        NUMERIC(14,2) NOT NULL DEFAULT 0,
    gst_rate        NUMERIC(5,2) NOT NULL DEFAULT 0,
    taxable_amount  NUMERIC(14,2) NOT NULL,
    tax_amount      NUMERIC(14,2) NOT NULL,
    line_total      NUMERIC(14,2) NOT NULL
);

CREATE TABLE sale_payments (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sale_id         UUID NOT NULL REFERENCES sales(id) ON DELETE CASCADE,
    method          payment_method NOT NULL,
    amount          NUMERIC(14,2) NOT NULL,
    reference       TEXT,                         -- txn id / last4 / UPI ref
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE sale_returns (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    store_id        UUID        NOT NULL REFERENCES stores(id),
    sale_id         UUID        NOT NULL REFERENCES sales(id),
    return_number   VARCHAR(30) NOT NULL,
    return_date     TIMESTAMPTZ NOT NULL DEFAULT now(),
    reason          TEXT,
    refund_method   payment_method,
    total_amount    NUMERIC(14,2) NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    UNIQUE (tenant_id, return_number)
);

CREATE TABLE sale_return_items (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sale_return_id  UUID NOT NULL REFERENCES sale_returns(id) ON DELETE CASCADE,
    sale_item_id    UUID REFERENCES sale_items(id),
    variant_id      UUID NOT NULL REFERENCES product_variants(id),
    quantity        NUMERIC(14,3) NOT NULL,
    unit_price      NUMERIC(14,2) NOT NULL,
    line_total      NUMERIC(14,2) NOT NULL
);

-- =============================================================================
-- 11. EXPENSES
-- =============================================================================
CREATE TABLE expense_categories (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    name            TEXT        NOT NULL,         -- Rent, Electricity, Salaries, Courier, Internet, Misc
    is_active       BOOLEAN     NOT NULL DEFAULT TRUE,
    UNIQUE (tenant_id, name)
);

CREATE TABLE expenses (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    store_id        UUID        REFERENCES stores(id),
    category_id     UUID        NOT NULL REFERENCES expense_categories(id),
    expense_date    DATE        NOT NULL DEFAULT CURRENT_DATE,
    amount          NUMERIC(14,2) NOT NULL,
    payment_method  payment_method NOT NULL DEFAULT 'Cash',
    vendor          TEXT,
    description     TEXT,
    receipt_document_id UUID,                     -- FK added after documents table
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    is_deleted      BOOLEAN     NOT NULL DEFAULT FALSE
);
CREATE INDEX ix_expenses_date ON expenses(tenant_id, expense_date);

-- =============================================================================
-- 12. DOCUMENT MANAGEMENT
-- =============================================================================
CREATE TABLE document_types (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    name            TEXT        NOT NULL,         -- GST Certificate, Rent Agreement, Aadhaar, Invoice...
    owner_type      document_owner_type NOT NULL,
    has_expiry      BOOLEAN     NOT NULL DEFAULT FALSE,
    UNIQUE (tenant_id, name)
);

CREATE TABLE documents (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    document_type_id UUID       REFERENCES document_types(id),
    owner_type      document_owner_type NOT NULL,
    owner_id        UUID,                         -- employee/supplier/customer/product id; NULL for business
    title           TEXT        NOT NULL,
    folder_path     TEXT        NOT NULL DEFAULT '/',   -- virtual folder structure
    current_version_id UUID,                      -- FK added after document_versions
    issue_date      DATE,
    expiry_date     DATE,
    ocr_text        TEXT,                          -- extracted text for search
    metadata        JSONB,                         -- OCR metadata, custom fields
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    updated_at      TIMESTAMPTZ,
    updated_by      UUID,
    is_deleted      BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at      TIMESTAMPTZ,
    deleted_by      UUID
);
CREATE INDEX ix_documents_owner   ON documents(owner_type, owner_id);
CREATE INDEX ix_documents_expiry  ON documents(expiry_date) WHERE expiry_date IS NOT NULL;
CREATE INDEX ix_documents_title   ON documents USING gin (title gin_trgm_ops);
CREATE INDEX ix_documents_ocr     ON documents USING gin (to_tsvector('english', coalesce(ocr_text,'')));

CREATE TABLE document_versions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id     UUID        NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    version_no      INT         NOT NULL,
    s3_key          TEXT        NOT NULL,
    file_name       TEXT        NOT NULL,
    content_type    TEXT        NOT NULL,
    size_bytes      BIGINT      NOT NULL,
    checksum_sha256 TEXT,
    virus_scan_status TEXT      NOT NULL DEFAULT 'Pending',  -- Pending,Clean,Infected
    uploaded_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    uploaded_by     UUID,
    UNIQUE (document_id, version_no)
);

CREATE TABLE tags (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    name            CITEXT      NOT NULL,
    UNIQUE (tenant_id, name)
);

CREATE TABLE document_tags (
    document_id     UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    tag_id          UUID NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
    PRIMARY KEY (document_id, tag_id)
);

-- Deferred FKs that point into documents
ALTER TABLE payrolls  ADD CONSTRAINT fk_payroll_slip_doc
    FOREIGN KEY (slip_document_id)     REFERENCES documents(id);
ALTER TABLE purchases ADD CONSTRAINT fk_purchase_invoice_doc
    FOREIGN KEY (invoice_document_id)  REFERENCES documents(id);
ALTER TABLE expenses  ADD CONSTRAINT fk_expense_receipt_doc
    FOREIGN KEY (receipt_document_id)  REFERENCES documents(id);
ALTER TABLE documents ADD CONSTRAINT fk_document_current_version
    FOREIGN KEY (current_version_id)   REFERENCES document_versions(id);

-- =============================================================================
-- 13. NOTIFICATIONS
-- =============================================================================
CREATE TABLE notifications (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID        NOT NULL REFERENCES tenants(id),
    type            notification_type NOT NULL,
    title           TEXT        NOT NULL,
    body            TEXT,
    channel         TEXT        NOT NULL DEFAULT 'Email',   -- Email, InApp
    recipient       TEXT,                                    -- email address / user id
    status          notification_status NOT NULL DEFAULT 'Queued',
    payload         JSONB,
    sent_at         TIMESTAMPTZ,
    error           TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX ix_notifications_status ON notifications(status, created_at);

-- =============================================================================
-- SEED — system roles & a starter permission set (insert per tenant on onboarding)
-- =============================================================================
-- Roles are created per-tenant by the onboarding command; permission codes below
-- are the global catalogue.
INSERT INTO permissions (code, description) VALUES
    ('dashboard.view',      'View dashboard'),
    ('product.view',        'View products'),
    ('product.manage',      'Create/update/delete products'),
    ('inventory.view',      'View inventory'),
    ('inventory.adjust',    'Adjust / transfer stock'),
    ('purchase.view',       'View purchases'),
    ('purchase.manage',     'Create/manage purchases & POs'),
    ('sale.create',         'Create sales / POS billing'),
    ('sale.refund',         'Process sale returns / refunds'),
    ('customer.manage',     'Manage customers'),
    ('supplier.manage',     'Manage suppliers'),
    ('employee.manage',     'Manage employees'),
    ('payroll.manage',      'Generate / approve payroll'),
    ('expense.manage',      'Manage expenses'),
    ('document.view',       'View documents'),
    ('document.manage',     'Upload / manage documents'),
    ('report.view',         'View & export reports'),
    ('user.manage',         'Manage users & roles'),
    ('audit.view',          'View audit logs')
ON CONFLICT (code) DO NOTHING;
