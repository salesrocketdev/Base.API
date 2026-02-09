# Architecture Checklist

Use this checklist before finalizing any feature:

- [ ] Keep the feature business-agnostic for boilerplate scope.
- [ ] Place code in correct layer (`Base.API`, `Base.Domain`, `Base.Infrastructure`, `Base.Core`).
- [ ] Keep DTOs and HTTP concerns out of domain services.
- [ ] Keep persistence concerns out of `Base.Domain` and `Base.Core`.
- [ ] If entity is tenant-scoped, include `CompanyId` and enforce tenant filters.
- [ ] Register dependencies in `Base.API/Extensions/ServiceCollectionExtensions.cs`.
- [ ] Keep configuration in `appsettings*.json` + env vars.
- [ ] Keep structured error/response contracts.
