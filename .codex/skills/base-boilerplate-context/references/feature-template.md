# Feature Template

Use this sequence:

1. Define scope
- Inputs, outputs, endpoints, and invariants.

2. Domain first
- Add/update entities/interfaces/services in `Base.Domain`.

3. Infrastructure
- Implement repositories and update `ApplicationDbContext` if needed.
- Add migration if model changed.

4. API
- Add/update DTOs and controller endpoints.
- Wire dependencies in DI extensions.

5. Cross-cutting
- Add logs, validation, and security checks.
- Ensure tenant context is respected.

6. Tests
- Add/update tests in `Base.Tests` for success and failure paths.

7. Verify
- Build, test, and run local/docker if change impacts startup/runtime.
