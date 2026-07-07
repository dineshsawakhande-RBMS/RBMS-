#!/usr/bin/env bash
# Populate the running RBMS API (http://localhost:5080) with rich demo data so every module
# has something to show: suppliers, purchases, customers, sales, employees, attendance, leaves,
# payroll, documents and notifications. API-driven, so all business rules (stock ledger,
# moving-avg cost, loyalty, payroll proration) are respected.
#
# Usage:  bash scripts/seed-demo-data.sh
# Reset first with scripts/reset-demo-data.sql if you want a clean base.
set -euo pipefail

API="${RBMS_API:-http://localhost:5080}"
MAIN_STORE="aaaaaaaa-0000-0000-0000-000000000002"

echo "Logging in..."
TOKEN=$(curl -s -X POST "$API/api/auth/login" -H "Content-Type: application/json" \
  -d '{"username":"owner","password":"Password123!"}' | python -c "import sys,json;print(json.load(sys.stdin)['accessToken'])")
AUTH="Authorization: Bearer $TOKEN"
post() { curl -s -X POST "$API/api/$1" -H "$AUTH" -H "Content-Type: application/json" -d "$2"; }
gid() { tr -d '"'; }   # strip quotes off a returned bare GUID

echo "Reading seeded product variants..."
# variantId|sku|sellingPrice for the first 6 variants at the main store
mapfile -t VARIANTS < <(curl -s "$API/api/inventory/levels?storeId=$MAIN_STORE&pageSize=50" -H "$AUTH" \
  | python -c "import sys,json;[print(f\"{r['variantId']}|{r['sku']}|{r['sellingPrice']}\") for r in json.load(sys.stdin)['items'][:6]]")
V0=${VARIANTS[0]%%|*}; V1=${VARIANTS[1]%%|*}; V2=${VARIANTS[2]%%|*}
V3=${VARIANTS[3]%%|*}; V4=${VARIANTS[4]%%|*}
price() { local i=$1; echo "${VARIANTS[$i]##*|}"; }

echo "Suppliers..."
S1=$(post suppliers '{"code":"SUP-STYLE","name":"Style Source Pvt Ltd","phone":"9820011111","city":"Surat","state":"GJ","paymentTermsDays":30,"openingBalance":0}' | gid)
S2=$(post suppliers '{"code":"SUP-WEAVE","name":"Weave & Co","phone":"9820022222","city":"Ludhiana","state":"PB","paymentTermsDays":15,"openingBalance":0}' | gid)
post suppliers '{"code":"SUP-TREND","name":"TrendLine Apparels","phone":"9820033333","city":"Delhi","state":"DL","paymentTermsDays":45,"openingBalance":0}' >/dev/null

echo "Purchases (stock in)..."
post purchases "{\"supplierId\":\"$S1\",\"storeId\":\"$MAIN_STORE\",\"invoiceNumber\":\"SS-1001\",\"invoiceDate\":\"2026-06-05\",\"discount\":0,\"amountPaid\":20000,\"items\":[{\"variantId\":\"$V0\",\"quantity\":40,\"unitCost\":520,\"gstRate\":12},{\"variantId\":\"$V1\",\"quantity\":30,\"unitCost\":610,\"gstRate\":12}]}" >/dev/null
post purchases "{\"supplierId\":\"$S2\",\"storeId\":\"$MAIN_STORE\",\"invoiceNumber\":\"WV-2002\",\"invoiceDate\":\"2026-06-20\",\"discount\":0,\"amountPaid\":0,\"items\":[{\"variantId\":\"$V2\",\"quantity\":25,\"unitCost\":700,\"gstRate\":12},{\"variantId\":\"$V3\",\"quantity\":20,\"unitCost\":450,\"gstRate\":12}]}" >/dev/null

echo "Customers..."
CUST=()
for pair in "Ananya Iyer|9900000001" "Rhea Kapoor|9900000002" "Meera Nair|9900000003" "Sara Khan|9900000004" "Diya Menon|9900000005" "Isha Verma|9900000006"; do
  name="${pair%%|*}"; mob="${pair##*|}"
  id=$(post customers "{\"name\":\"$name\",\"mobile\":\"$mob\"}" | gid)
  CUST+=("$id")
done

echo "Sales (POS)..."
sale() { # storeId customerId  var qty price
  local cust="$1" var="$2" qty="$3" pr="$4"
  local base=$(python -c "print(round($qty*$pr))")
  local total=$(python -c "print(round($qty*$pr*1.12))")
  local cj="null"; [ -n "$cust" ] && cj="\"$cust\""
  post sales "{\"storeId\":\"$MAIN_STORE\",\"customerId\":$cj,\"discount\":0,\"items\":[{\"variantId\":\"$var\",\"quantity\":$qty,\"unitPrice\":$pr,\"discount\":0,\"gstRate\":12}],\"payments\":[{\"method\":\"Cash\",\"amount\":$total,\"reference\":null}]}" >/dev/null
}
# repeat buyers (retention), plus walk-ins
sale "${CUST[0]}" "$V0" 2 "$(price 0)"
sale "${CUST[0]}" "$V1" 1 "$(price 1)"
sale "${CUST[0]}" "$V2" 3 "$(price 2)"
sale "${CUST[1]}" "$V1" 2 "$(price 1)"
sale "${CUST[1]}" "$V3" 1 "$(price 3)"
sale "${CUST[2]}" "$V0" 1 "$(price 0)"
sale "${CUST[2]}" "$V2" 2 "$(price 2)"
sale "${CUST[3]}" "$V3" 4 "$(price 3)"
sale "${CUST[4]}" "$V1" 1 "$(price 1)"
sale "${CUST[5]}" "$V0" 2 "$(price 0)"
sale "" "$V2" 1 "$(price 2)"
sale "" "$V3" 2 "$(price 3)"
sale "${CUST[1]}" "$V0" 1 "$(price 0)"
sale "${CUST[0]}" "$V3" 1 "$(price 3)"

echo "Employees..."
EMP=()
for row in "EMP-101|Priya Nair|9876500001|Store Manager|45000|2025-03-01" \
           "EMP-102|Kavya Rao|9876500002|Sales Associate|22000|2025-08-15" \
           "EMP-103|Farah Sheikh|9876500003|Cashier|20000|2026-01-10" \
           "EMP-104|Neha Gupta|9876500004|Tailor|24000|2026-02-01"; do
  IFS='|' read -r code name mob desig ctc doj <<< "$row"
  id=$(post employees "{\"employeeCode\":\"$code\",\"fullName\":\"$name\",\"mobile\":\"$mob\",\"designation\":\"$desig\",\"department\":\"Store\",\"joiningDate\":\"$doj\",\"monthlyCtc\":$ctc}" | gid)
  EMP+=("$id")
done

echo "Attendance (current month, two employees)..."
attend() { # employeeId
  local emp="$1"
  local entries
  entries=$(python -c "
import json
e=[]
for d in range(1,25):
    st='Present'
    if d in (7,14,21): st='WeekOff'
    elif d in (9,18): st='Absent'
    elif d in (12,): st='HalfDay'
    e.append({'workDate':f'2026-07-{d:02d}','status':st,'checkIn':None,'checkOut':None,'remarks':None})
print(json.dumps({'employeeId':'$emp','entries':e}))
")
  post attendance "$entries" >/dev/null
}
attend "${EMP[0]}"
attend "${EMP[1]}"

echo "Leaves (one pending, one approved)..."
post leaves "{\"employeeId\":\"${EMP[2]}\",\"leaveType\":\"Casual\",\"fromDate\":\"2026-07-28\",\"toDate\":\"2026-07-29\",\"reason\":\"Family function\"}" >/dev/null
LV=$(post leaves "{\"employeeId\":\"${EMP[3]}\",\"leaveType\":\"Sick\",\"fromDate\":\"2026-07-15\",\"toDate\":\"2026-07-16\",\"reason\":\"Fever\"}" | gid)
post "leaves/$LV/decide" '{"approve":true,"decisionNotes":"Approved"}' >/dev/null

echo "Salary (advance + payroll for last month)..."
post payroll/advances "{\"employeeId\":\"${EMP[1]}\",\"amount\":5000,\"advanceDate\":\"2026-07-05\",\"notes\":\"Festival advance\"}" >/dev/null
post payroll/generate "{\"employeeId\":\"${EMP[0]}\",\"periodYear\":2026,\"periodMonth\":6,\"workingDays\":26,\"presentDays\":26,\"bonus\":2000,\"deductions\":0}" >/dev/null
post payroll/generate "{\"employeeId\":\"${EMP[1]}\",\"periodYear\":2026,\"periodMonth\":6,\"workingDays\":26,\"presentDays\":24,\"bonus\":0,\"deductions\":500}" >/dev/null

echo "Documents..."
doc() { # title type expiry filename
  local tmp="/tmp/seed-$4"
  printf '%%PDF-1.4 RBMS demo document: %s' "$1" > "$tmp"
  curl -s -X POST "$API/api/documents" -H "$AUTH" \
    -F "file=@$tmp;type=application/pdf" -F "title=$1" -F "documentType=$2" \
    -F "expiryDate=$3" -F "tags=demo,$2" >/dev/null
  rm -f "$tmp"
}
doc "GST Registration Certificate" "GstCertificate" "2026-08-10" "gst.pdf"
doc "Shop Rent Agreement" "RentAgreement" "2027-03-31" "rent.pdf"
doc "Trade License 2025" "License" "2026-06-30" "license.pdf"
doc "Fire Safety Insurance" "Insurance" "2026-08-05" "insurance.pdf"

echo "Refreshing notifications..."
post notifications/refresh '' >/dev/null

echo "Done. Demo data seeded."
