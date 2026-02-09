# Base Boilerplate API (.NET 10 LTS)

Boilerplate para iniciar APIs .NET multi-tenant com autenticação completa, logging estruturado, seeders e PostgreSQL via Docker.

## Arquitetura

Camadas da solution:
- `Base.API`: controllers, middleware, composição de DI, configuração de segurança e pipeline HTTP.
- `Base.Domain`: entidades, contratos de repositórios e serviços de domínio.
- `Base.Infrastructure`: EF Core, DbContext, repositórios, UnitOfWork, migrations e seeders.
- `Base.Core`: componentes cross-cutting (JWT, hash de senha, email, tenant contracts, helpers).
- `Base.Tests`: testes de sanidade do boilerplate (auth, company e seeding).

Fluxo de requisição (resumo):
1. Request entra em `Base.API`.
2. Middleware de autenticação valida JWT.
3. `TenantMiddleware` resolve escopo de tenant (`CompanyId`).
4. Serviços de domínio usam `IUnitOfWork` + repositórios.
5. `TenantScopedRepository<T>` aplica isolamento por tenant para entidades com `CompanyId`.

## O que já vem pronto

- Auth completo:
  - signup/login/me/refresh/logout/forgot-password/reset-password
- Multi-tenant:
  - contexto de tenant (`ITenantContext`) + middleware + repository scoping
- Estrutura base de negócio:
  - `Company` e `CompanyMember`
- Observabilidade:
  - Serilog + Seq + persistência de exceptions em `AppLogs`
- Seeders idempotentes:
  - admin user e company inicial
- Email utility:
  - contrato e implementação com integração configurável
- Banco e execução:
  - EF Core migrations + Docker Compose (`api`, `db`, `migrator`, `seq`)

## Stack técnica

- .NET SDK: `10.0.102` (via `global.json`)
- ASP.NET Core: `10.0.2`
- EF Core: `10.0.2`
- PostgreSQL 16
- Hangfire (configurável)
- xUnit para testes

## Sistema de Email e Enqueue

Provider atual:
- ZeptoMail (`Base.Core/Email/SendMailService.cs`)

Como funciona:
1. Serviços de domínio disparam métodos de enqueue via `ISendMailService`.
2. `SendMailService` usa `BackgroundJob.Enqueue(...)` (Hangfire) para envio assíncrono.
3. Job executa envio HTTP para API de template do ZeptoMail.

Onde o enqueue é usado hoje:
- Boas-vindas no cadastro: `Base.Domain/Services/AuthService.cs` (`EnqueueWelcomeEmail`)
- Código/token de reset: `Base.Domain/Services/AuthService.cs` (`EnqueueVerificationCodeEmail`)

Configuração necessária:
- `Base.API/appsettings.json`:
  - `ZeptoMailSettings:Url`
  - `ZeptoMailSettings:Token`
  - `ZeptoMailSettings:FromAddress`
  - `ZeptoMailSettings:FromName`
- `.env.example` / `docker-compose.yml`:
  - `ZEPTOMAILSETTINGS__TOKEN`
- Hangfire (fila):
  - `Hangfire:Enabled` (habilita servidor de jobs)
  - `Hangfire:UseDashboard` (dashboard em dev)

Comportamento em desenvolvimento:
- Sem token válido, o serviço entra em modo de desenvolvimento e loga o payload do email ao invés de enviar.
- Falha ao enfileirar/enviar não quebra o fluxo principal de auth (email é tratado como não crítico).

Observação importante:
- As `mail_template_key` e URL de app usadas no corpo dos emails ainda estão como placeholders/TODO no `SendMailService`.
- Em projetos derivados, substitua template keys, domínio e variáveis de merge para seu ambiente.

## Subir local (sem Docker)

1. Copie `.env.example` para `.env` e ajuste segredos.
2. Defina a connection string via ambiente (`ConnectionStrings__DefaultConnection`) se necessário.
3. Rode:
   - Windows: `./run-dev.ps1`
   - Linux/macOS: `./run-dev.sh`

## Subir com Docker

1. Copie `.env.example` para `.env`.
2. Rode: `docker compose up --build -d`
3. Endpoints:
   - API: `http://localhost:5000`
   - Seq: `http://localhost:5342`

## Testes

- Todos os testes: `dotnet test Base.Boilerplate.sln`

## Convenções para evoluir o boilerplate

- Evite acoplar regra de negócio específica neste repositório.
- Mantenha novos módulos como "feature packs" plugáveis.
- Toda entidade tenant-scoped deve conter `CompanyId`.
- Atualize migration sempre que alterar modelo EF.
- Prefira manter segredos via variáveis de ambiente.

## Estrutura de apoio para IA (MCP + Skill)

Este repositório inclui:
- `.mcp.json`: configuração de servidor MCP de filesystem para fornecer contexto do projeto a assistentes.
- `.codex/skills/base-boilerplate-context/SKILL.md`: skill para guiar implementações futuras sem quebrar o boilerplate.
- `.codex/skills/base-boilerplate-context/references/email-system.md`: referência do fluxo de email, enqueue e configuração.

Use essa skill quando pedir novas features para preservar arquitetura, multi-tenant, auth, logs e testes.
