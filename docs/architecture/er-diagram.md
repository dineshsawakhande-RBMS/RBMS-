# Entity-Relationship Diagram

Derived from [`docs/database/schema.sql`](../database/schema.sql). Cross-cutting columns
(`tenant_id`, audit, soft-delete) are omitted from the diagram for readability — every
business table carries them.

## Core domain (Products → Inventory → Purchase → Sales)

```mermaid
erDiagram
    TENANTS ||--o{ STORES : has
    TENANTS ||--o{ USERS : has
    STORES  ||--o{ INVENTORY : holds

    CATEGORIES ||--o{ PRODUCTS : classifies
    BRANDS     ||--o{ PRODUCTS : labels
    PRODUCTS   ||--o{ PRODUCT_VARIANTS : "has variants"
    PRODUCTS   ||--o{ PRODUCT_IMAGES : "has images"
    PRODUCT_VARIANTS ||--o{ PRODUCT_IMAGES : "variant image"

    PRODUCT_VARIANTS ||--o{ INVENTORY : "stocked as"
    PRODUCT_VARIANTS ||--o{ STOCK_MOVEMENTS : "moves"
    STORES           ||--o{ STOCK_MOVEMENTS : "at"

    SUPPLIERS ||--o{ PURCHASE_ORDERS : "ordered from"
    PURCHASE_ORDERS ||--o{ PURCHASE_ORDER_ITEMS : contains
    SUPPLIERS ||--o{ PURCHASES : "invoiced by"
    PURCHASE_ORDERS ||--o{ PURCHASES : "received as"
    PURCHASES ||--o{ PURCHASE_ITEMS : contains
    PURCHASES ||--o{ PURCHASE_RETURNS : "returned via"
    PURCHASE_RETURNS ||--o{ PURCHASE_RETURN_ITEMS : contains
    PRODUCT_VARIANTS ||--o{ PURCHASE_ITEMS : "purchased"

    CUSTOMERS ||--o{ SALES : "buys"
    USERS     ||--o{ SALES : "cashier"
    SALES ||--o{ SALE_ITEMS : contains
    SALES ||--o{ SALE_PAYMENTS : "paid by"
    SALES ||--o{ SALE_RETURNS : "returned via"
    SALE_RETURNS ||--o{ SALE_RETURN_ITEMS : contains
    PRODUCT_VARIANTS ||--o{ SALE_ITEMS : "sold"

    CUSTOMERS ||--o{ LOYALTY_TRANSACTIONS : earns

    PRODUCT_VARIANTS {
        uuid id PK
        uuid product_id FK
        string sku
        string barcode
        string size
        string color
        numeric purchase_price
        numeric selling_price
        numeric reorder_level
    }
    INVENTORY {
        uuid id PK
        uuid store_id FK
        uuid variant_id FK
        numeric quantity_on_hand
        numeric avg_cost
    }
    STOCK_MOVEMENTS {
        uuid id PK
        uuid variant_id FK
        enum movement_type
        numeric quantity
        numeric balance_after
        string reference_type
        uuid reference_id
    }
```

## Security & multi-tenancy

```mermaid
erDiagram
    TENANTS ||--o{ ROLES : defines
    TENANTS ||--o{ USERS : owns
    ROLES   ||--o{ ROLE_PERMISSIONS : grants
    PERMISSIONS ||--o{ ROLE_PERMISSIONS : "granted in"
    USERS   ||--o{ USER_ROLES : assigned
    ROLES   ||--o{ USER_ROLES : "assigned to"
    USERS   ||--o{ REFRESH_TOKENS : issues
    USERS   ||--o{ LOGIN_HISTORY : records
    REFRESH_TOKENS ||--o| REFRESH_TOKENS : "rotated to"
```

## People & payroll

```mermaid
erDiagram
    TENANTS ||--o{ EMPLOYEES : employs
    USERS   ||--o| EMPLOYEES : "may log in as"
    EMPLOYEES ||--o{ EMPLOYEE_BANK_DETAILS : has
    EMPLOYEES ||--o{ ATTENDANCE : records
    EMPLOYEES ||--o{ LEAVES : requests
    EMPLOYEES ||--o{ SALARY_STRUCTURES : "paid per"
    SALARY_STRUCTURES ||--o{ SALARY_STRUCTURE_COMPONENTS : "made of"
    EMPLOYEES ||--o{ SALARY_ADVANCES : takes
    EMPLOYEES ||--o{ PAYROLLS : "paid via"
    PAYROLLS  ||--o{ PAYROLL_LINES : "broken into"
    PAYROLLS  ||--o| DOCUMENTS : "slip PDF"
```

## Documents (polymorphic owner)

```mermaid
erDiagram
    TENANTS ||--o{ DOCUMENTS : owns
    DOCUMENT_TYPES ||--o{ DOCUMENTS : categorizes
    DOCUMENTS ||--o{ DOCUMENT_VERSIONS : versions
    DOCUMENTS ||--o| DOCUMENT_VERSIONS : "current"
    DOCUMENTS ||--o{ DOCUMENT_TAGS : tagged
    TAGS ||--o{ DOCUMENT_TAGS : "applied as"
    DOCUMENTS {
        uuid id PK
        enum owner_type "Business|Employee|Supplier|Customer|Product"
        uuid owner_id "polymorphic"
        string folder_path
        date expiry_date
        text ocr_text
        jsonb metadata
    }
```

> **Polymorphic ownership:** `documents.owner_type` + `owner_id` lets one document store
> serve business certificates, employee paperwork, supplier files, and product assets
> without separate tables. Application code resolves `owner_id` per `owner_type`.
