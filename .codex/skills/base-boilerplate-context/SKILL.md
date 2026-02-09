---
name: base-boilerplate-context
description: Preserve and extend this .NET API boilerplate architecture. Use when adding or modifying features to keep clean layering (Base.API/Base.Domain/Base.Infrastructure/Base.Core), tenant isolation, authentication flows, logging, seeders, migrations, Docker compatibility, and test coverage without introducing CRM-specific business rules.
---

# Base Boilerplate Context

Follow this workflow when implementing features.

## 1) Confirm boundaries
- Keep business-agnostic scope.
- Reject or isolate CRM-specific behavior.
- Keep responsibilities split by layer.

## 2) Apply architectural rules
- API layer: endpoints, DTO mapping, middleware, DI wiring.
- Domain layer: entities, contracts, use-case services.
- Infrastructure layer: EF model/repositories/UnitOfWork/seeding/migrations.
- Core layer: reusable cross-cutting utilities only.

## 3) Preserve non-functional guarantees
- Keep JWT auth and refresh flow intact.
- Keep tenant scoping (`CompanyId`) enforced for tenant entities.
- Keep structured logging and exception persistence.
- Keep Docker flow (`api`, `db`, `migrator`, `seq`) operational.

## 4) Required validation
- Build: `dotnet build Base.Boilerplate.sln`
- Tests: `dotnet test Base.Boilerplate.sln`
- If model changed: create/update migration in `Base.Infrastructure/Migrations`.

## 5) Pull references only when needed
- Architecture checklist: `references/architecture-checklist.md`
- Feature template: `references/feature-template.md`
- Boilerplate quality gate: `references/quality-gate.md`
- Email system and enqueue flow: `references/email-system.md`
