-- Reset RBMS demo/transaction data to a clean slate (keeps tenant, users, products,
-- variants). Re-seeds opening stock of 25 per active variant at the seeded store.
-- Usage (PowerShell):
--   $env:PGPASSWORD='<pw>'; & 'C:\Program Files\PostgreSQL\16\bin\psql.exe' -U postgres -d rbms -f scripts/reset-demo-data.sql
BEGIN;

TRUNCATE whatsapp_messages, notifications, attendance, leaves,
         payroll_lines, payrolls, salary_advances,
         sale_payments, sale_return_items, sale_returns, sale_items, sales,
         purchase_return_items, purchase_returns, purchase_items, purchases,
         supplier_ledger, suppliers, loyalty_transactions, customers, employees,
         documents,
         product_images, stock_adjustment_lines, stock_adjustments, stock_movements, inventory
CASCADE;

INSERT INTO inventory (id, tenant_id, store_id, variant_id, quantity_on_hand, avg_cost, updated_at)
SELECT gen_random_uuid(), v.tenant_id, 'aaaaaaaa-0000-0000-0000-000000000002', v.id, 25, v.purchase_price, now()
FROM product_variants v WHERE v.is_deleted = false;

INSERT INTO stock_movements (id, tenant_id, store_id, variant_id, movement_type, quantity, unit_cost, balance_after, reference_type, notes, created_at)
SELECT gen_random_uuid(), v.tenant_id, 'aaaaaaaa-0000-0000-0000-000000000002', v.id, 'OpeningStock', 25, v.purchase_price, 25, 'OpeningStock', 'Reset opening balance', now()
FROM product_variants v WHERE v.is_deleted = false;

COMMIT;
