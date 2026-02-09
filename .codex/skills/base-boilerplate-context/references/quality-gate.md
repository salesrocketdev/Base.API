# Quality Gate

A change is only complete when all items pass:

- [ ] `dotnet build Base.Boilerplate.sln` passes.
- [ ] `dotnet test Base.Boilerplate.sln` passes.
- [ ] No new references to legacy names.
- [ ] No `bin/` or `obj/` files tracked by git.
- [ ] Docker compose still starts `db`, `migrator`, `api`, `seq`.
- [ ] README remains aligned with architecture and startup steps.
