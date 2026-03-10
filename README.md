# Base API Boilerplate (.NET 10)

Boilerplate de API multi-tenant com JWT, PostgreSQL, EF Core e fluxo base de autenticacao e company.

## Endpoints

Auth:

- `POST /api/auth/signup`
- `POST /api/auth/login`
- `POST /api/auth/login/initiate`
- `POST /api/auth/refresh`
- `POST /api/auth/switch-organization`
- `POST /api/auth/logout`
- `GET /api/auth/me`
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`
- `POST /api/auth/first-access/complete`
- `POST /api/auth/first-access/resend`

Observacoes:

- `switch-organization` retorna novo access token com claim `org_pid` da organizacao ativa.
- `forgot-password` e `first-access/resend` sempre retornam mensagem generica.
- `reset-password` e `first-access/complete` usam OTP de 6 digitos.

Company:

- `POST /api/company`
- `GET /api/company/{id}`
- `GET /api/company/me`
- `PUT /api/company/me`
- `POST /api/company/members/invite`
- `PUT /api/company/{id}`
- `DELETE /api/company/{id}`

## Configuracao

Variaveis importantes:

- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `POSTGRES_DB`
- `JWT__SECRETKEY`
- `JWT__ISSUER`
- `JWT__AUDIENCE`
- `JWT__ACCESSTOKENEXPIRATIONMINUTES`
- `JWT__REFRESHTOKENEXPIRATIONDAYS`
- `SECURITY__PASSWORDHASHING__ITERATIONS`
- `SECURITY__PASSWORDRESET__PEPPER`
- `SEED__ENABLED`
- `SEED__SCENARIO`
- `SEED__RESETBEFORESEED`
- `SEED__ADMINEMAIL`
- `SEED__ADMINPASSWORD`
- `SEED__ADMINNAME`
- `SEED__LEARNEREMAIL`
- `SEED__LEARNERPASSWORD`
- `SEED__LEARNERNAME`
- `SEED__ORGANIZATIONNAME`
- `SEED__ORGANIZATIONSLUG`
- `DATABASE__APPLYMIGRATIONSONSTARTUP`
- `HANGFIRE__STORAGE`
- `HANGFIRE__CONNECTIONSTRING`

## Execucao local

Sem Docker:

1. Copie `.env.example` para `.env`.
2. Ajuste os segredos e a conexao com banco.
3. Execute `./run-dev.ps1` (Windows) ou `./run-dev.sh` (Linux/macOS).

Com Docker:

1. Copie `.env.example` para `.env`.
2. Execute `docker compose up --build -d`.
3. Endpoints:
   - API: `http://localhost:5000`
   - Seq: `http://localhost:5342`

Notas de runtime:

- Com `docker-compose`, as migrations rodam pelo servico `migrator` (API com `Database__ApplyMigrationsOnStartup=false`).
- Fora do Docker, controle de migration via `Database:ApplyMigrationsOnStartup`.

## Build e testes

- Build: `dotnet build`
- Testes: `dotnet test`
