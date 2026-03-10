---
name: ef-query-optimization
description: Analyze and optimize Entity Framework and SQL query performance in .NET APIs. Use when touching repositories/services/DbContext, reviewing slow endpoints, investigating high DB CPU/latency, or when query anti-patterns are likely (GetAll+filter in memory, missing tenant filters, N+1, overfetch, non-sargable predicates, missing indexes, excessive tracking).
---

# EF Query Optimization

Use this skill to proactively detect and improve query performance.

## 1) Operate autonomously
- Run a quick scan for query anti-patterns before changing data-access code.
- Treat performance issues as defects when they create clear waste or scalability risk.
- Implement safe optimizations directly.
- Ask before changing behavior, schema semantics, or returning different result shapes.

## 2) Keep architecture boundaries
- Keep persistence logic in `Infrastructure` repositories and EF mappings.
- Keep business rules in `Domain` services.
- Avoid leaking DTO/HTTP concerns into repositories.
- Preserve tenant isolation (`OrganizationId`) in all tenant-scoped queries.

## 3) Detection workflow (always)
1. Map the read/write path from endpoint -> domain service -> repository -> EF query.
2. Identify anti-patterns using search and code inspection.
3. Rank severity: `critical`, `high`, `medium`, `low`.
4. Fix highest-impact items first.
5. Validate with build/tests and query-level checks.

Use fast scans:
- `rg -n "GetAllAsync\(|ToListAsync\(|Include\(|ThenInclude\(|ToLower\(|AsNoTracking\(|Skip\(|Take\(" Eradia.*`
- `rg -n "Where\(.*OrganizationId|OrganizationId ==" Eradia.*`
- `rg -n "for(each)? .*await|foreach .*await" Eradia.*`

## 4) Severity model
- `critical`
  - Materializing whole tables (`GetAllAsync`/`ToListAsync`) and filtering in memory for request paths.
  - Missing tenant filter in tenant-scoped query.
  - Clear N+1 query loop in hot path.
- `high`
  - Overfetch with broad `Include` where projection is enough.
  - Read queries without `AsNoTracking`.
  - Non-sargable predicates on indexed columns (`ToLower`, `Substring`, `%like%` leading wildcard).
- `medium`
  - Missing pagination for potentially large lists.
  - Redundant `SaveChanges` round-trips inside loops.
- `low`
  - Minor ordering/index alignment issues with low cardinality impact.

## 5) Optimization playbook

### A) Push filtering to database
- Replace `GetAllAsync().Where(...)` with repository methods that filter in SQL.
- Add targeted repository methods instead of generic in-memory filtering.

### B) Add projection-first reads
- Prefer `Select(...)` over heavy `Include(...)` when only a subset is needed.
- Use `AsNoTracking()` for read-only endpoints.

### C) Remove N+1 patterns
- Batch-load related data.
- Convert loops with per-item query into one set-based query.

### D) Keep predicates index-friendly
- Avoid applying functions to indexed columns in predicate.
- Normalize input instead of transforming DB column (`slugNormalized == inputNormalized`).
- For case-insensitive matching, prefer normalized columns or DB-native index strategy.

### E) Align indexes with query patterns
- Validate composite index order matches filter + order usage.
- For frequent filters, add or adjust indexes in `ApplicationDbContext` mapping and migration.

### F) Control result size
- Add `Take(...)` and optional paging for list endpoints.
- Enforce upper bounds.

## 6) EF-specific guidance
- Use `AsNoTracking()` on read paths.
- Use `AnyAsync()` for existence checks, not `CountAsync() > 0`.
- Use `FirstOrDefaultAsync()` only when one row is expected.
- Avoid multiple enumerations of `IQueryable`.
- Prefer `ExecuteUpdateAsync`/`ExecuteDeleteAsync` (when available and safe) for set-based updates/deletes.

## 7) SQL-specific guidance
- Confirm generated SQL shape for complex LINQ.
- Check for scans, hash joins, sort spills, and bad row estimates with `EXPLAIN (ANALYZE, BUFFERS)` in Postgres.
- Validate that parameterized SQL is used (avoid dynamic SQL concatenation).

## 8) Multi-tenant safety checks
- Every tenant-scoped query must constrain `OrganizationId` (or equivalent tenancy key).
- Never relax tenant filter for convenience.
- Keep RBAC and membership checks unchanged while optimizing queries.

## 9) Output contract when using this skill
- Report findings first, ordered by severity.
- Include file references and precise anti-pattern.
- State concrete fix and expected impact.
- Mention residual risk if no benchmark is available.

## 10) Validation checklist
- `dotnet build Eradia.Boilerplate.sln`
- `dotnet test Eradia.Boilerplate.sln`
- If mapping/index changed: generate migration and verify startup path.
- If query changed in hot path: verify SQL plan with representative data.

Load `references/ef-query-checklist.md` when you need detailed anti-pattern mapping and fix templates.
