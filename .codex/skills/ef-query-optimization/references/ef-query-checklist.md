# EF Query Checklist

Use this checklist for fast diagnosis and safe remediation.

## 1) Anti-pattern -> Fix

- `GetAllAsync().Where(...)` in domain/service
  - Fix: Add repository method with SQL filter and use it directly.

- Missing tenant filter (`OrganizationId`) on tenant-scoped reads/writes
  - Fix: Enforce filter in repository query and verify membership flow remains intact.

- `Include` chains with wide object graph for simple DTO
  - Fix: Replace with projection (`Select`) returning only required fields.

- Read queries with default tracking
  - Fix: Add `AsNoTracking()` unless entities will be updated in same context.

- Query in loop (`foreach` + `await repository...`)
  - Fix: Batch query once and map in-memory dictionary.

- `ToLower()`/function on DB column in predicate
  - Fix: Normalize input and compare directly, or adopt DB-native case-insensitive strategy.

- List endpoints without upper bounds
  - Fix: Add pagination (`Skip`/`Take`) and cap max page size.

- Multiple `SaveChangesAsync` in same transaction path
  - Fix: Collapse into fewer commits when semantics allow.

## 2) Index verification

Before adding index:
- Confirm query pattern frequency and cardinality.
- Confirm existing index coverage and order.

When adding index:
- Match filter/order sequence.
- Keep composite indexes tight.
- Add migration and test on realistic dataset.

## 3) Query plan signals (Postgres)

Red flags:
- Sequential scan on large tables for high-frequency path.
- Large rows removed by filter.
- Repeated nested loops over large sets.
- Expensive sort with high memory/time.

Recommended command:
- `EXPLAIN (ANALYZE, BUFFERS) <query>;`

## 4) Safety guardrails

- Preserve response shape unless user asked for contract changes.
- Preserve business rules and authorization checks.
- Preserve tenant isolation in all query rewrites.
- Add/adjust tests when query behavior changes.
