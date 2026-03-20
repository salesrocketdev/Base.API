# Base API Boilerplate (.NET 10)

Boilerplate de API `single-company` com JWT, PostgreSQL, EF Core, seeding, logging, Hangfire e arquitetura em camadas.

O objetivo desta base e servir como ponto de partida para novos produtos sem misturar regra de negocio especifica do cliente dentro da estrutura principal.

## Visao geral

Hoje o projeto esta organizado assim:

```text
Base.API
Base.Application
Base.Domain
Base.Infrastructure
Base.Core
Base.Tests
```

Fluxo padrao de execucao:

1. A requisicao entra pela `Base.API`.
2. O controller chama um caso de uso da `Base.Application`.
3. A `Base.Application` orquestra o fluxo usando contratos do `Base.Domain`.
4. A `Base.Infrastructure` implementa persistencia, EF Core, repositories, seeding e migrations.
5. A `Base.Core` fornece componentes tecnicos reutilizaveis, como JWT, hashing e email.

## Camadas

### `Base.API`

Responsavel por:

- Controllers
- DTOs de request/response
- Middleware
- Bootstrap da aplicacao
- Registro de dependencias
- Configuracao HTTP, auth, CORS, rate limiting e pipeline

Exemplos:

- [Program.cs](/C:/dev/Base.API/Base.API/Program.cs)
- [ServiceCollectionExtensions.cs](/C:/dev/Base.API/Base.API/Extensions/ServiceCollectionExtensions.cs)
- [AuthController.cs](/C:/dev/Base.API/Base.API/Controllers/AuthController.cs)
- [CompanyController.cs](/C:/dev/Base.API/Base.API/Controllers/CompanyController.cs)

Nao deve conter:

- Regra de negocio profunda
- Logica de persistencia
- Regras de EF Core

### `Base.Application`

Responsavel por:

- Casos de uso
- Orquestracao de fluxo
- Coordinacao entre repositories, token, hashing, email e transacao

Exemplos:

- [AuthService.cs](/C:/dev/Base.API/Base.Application/Services/AuthService.cs)
- [CompanyService.cs](/C:/dev/Base.API/Base.Application/Services/CompanyService.cs)
- [IAuthService.cs](/C:/dev/Base.API/Base.Application/Interfaces/Services/IAuthService.cs)
- [ICompanyService.cs](/C:/dev/Base.API/Base.Application/Interfaces/Services/ICompanyService.cs)

Nao deve conter:

- Controllers
- DTOs HTTP
- Detalhes de EF Core

### `Base.Domain`

Responsavel por:

- Entidades
- Contratos centrais
- Interfaces de repositories
- `IUnitOfWork`
- Constantes do dominio

Exemplos:

- [User.cs](/C:/dev/Base.API/Base.Domain/Entities/User.cs)
- [Company.cs](/C:/dev/Base.API/Base.Domain/Entities/Company.cs)
- [CompanyMember.cs](/C:/dev/Base.API/Base.Domain/Entities/CompanyMember.cs)
- [IUnitOfWork.cs](/C:/dev/Base.API/Base.Domain/Interfaces/IUnitOfWork.cs)

Nao deve conter:

- Controllers
- DTOs de API
- Implementacao de banco

### `Base.Infrastructure`

Responsavel por:

- `ApplicationDbContext`
- Repositories
- `UnitOfWork`
- Migrations
- Seeding
- Suporte de configuracao para EF design-time

Exemplos:

- [ApplicationDbContext.cs](/C:/dev/Base.API/Base.Infrastructure/Data/ApplicationDbContext.cs)
- [UnitOfWork.cs](/C:/dev/Base.API/Base.Infrastructure/UnitOfWork.cs)
- [UserRepository.cs](/C:/dev/Base.API/Base.Infrastructure/Repositories/UserRepository.cs)
- [Migrations](/C:/dev/Base.API/Base.Infrastructure/Migrations)

Nao deve conter:

- Controllers
- Regra de negocio HTTP

### `Base.Core`

Responsavel por componentes tecnicos reutilizaveis e independentes de modulo:

- JWT
- Hashing de senha
- Protecao de OTP
- Email
- Helpers

Exemplos:

- [JwtTokenGenerator.cs](/C:/dev/Base.API/Base.Core/Security/JwtTokenGenerator.cs)
- [HybridPasswordHasher.cs](/C:/dev/Base.API/Base.Core/Security/HybridPasswordHasher.cs)
- [SendMailService.cs](/C:/dev/Base.API/Base.Core/Email/SendMailService.cs)

### `Base.Tests`

Responsavel por validar controllers, services, middleware, seeding e fluxos principais.

Exemplos:

- [AuthControllerTests.cs](/C:/dev/Base.API/Base.Tests/AuthControllerTests.cs)
- [AuthServicePasswordResetTests.cs](/C:/dev/Base.API/Base.Tests/AuthServicePasswordResetTests.cs)
- [CompanyControllerTests.cs](/C:/dev/Base.API/Base.Tests/CompanyControllerTests.cs)
- [TenantMiddlewareTests.cs](/C:/dev/Base.API/Base.Tests/TenantMiddlewareTests.cs)

## Estrutura atual

```text
Base.API/
  Controllers/
  DTOs/
  Extensions/
  Middleware/
  Tenant/
  Program.cs

Base.Application/
  Interfaces/Services/
  Services/

Base.Domain/
  Constants/
  Entities/
  Interfaces/

Base.Infrastructure/
  Configuration/
  Data/
  Migrations/
  Repositories/
  Seeding/
  UnitOfWork.cs

Base.Core/
  Configuration/
  Email/
  Helpers/
  Security/
  Tenant/

Base.Tests/
```

## Modelo single-company

Este boilerplate opera em modo `single-company`.

Isso significa:

- cada usuario possui uma company atual em `User.CompanyId`
- o contexto da company e resolvido a partir do usuario autenticado
- `POST /api/company` retorna conflito se o usuario ja pertence a uma company
- `DELETE /api/company/{id}` nao e suportado neste modo

Se uma entidade for escopada por company, ela deve carregar `CompanyId` e ser tratada como dado tenant-aware desde a modelagem.

## Como renomear o boilerplate

Como este repositorio e um boilerplate, o primeiro passo em um projeto novo normalmente e trocar o prefixo `Base` pelo nome real do produto.

Para isso existem dois scripts prontos:

- `scripts/rename-project.sh`
- `scripts/rename-project.ps1`

Eles fazem tres tipos de ajuste:

- renomeiam pastas e arquivos com prefixo `Base`
- atualizam ocorrencias textuais de `Base` em arquivos de codigo e configuracao
- executam `dotnet build` e `dotnet test` no final por padrao

Exemplos:

```bash
./scripts/rename-project.sh Eradia
./scripts/rename-project.sh Eradia Base --dry-run
./scripts/rename-project.sh Eradia Base --code-only --skip-validation
```

```powershell
./scripts/rename-project.ps1 -NewName Eradia
./scripts/rename-project.ps1 -NewName Eradia -OldName Base -WhatIf
./scripts/rename-project.ps1 -NewName Eradia -CodeOnly -SkipValidation
```

### O que o script faz

- troca nomes como `Base.API`, `Base.Core`, `Base.Domain` e `Base.Boilerplate.sln`
- troca ocorrencias isoladas da palavra `Base` dentro de `.cs`, `.csproj`, `.sln`, `.json`, `.yml`, `.ps1`, `.sh`, `.md` e outros arquivos textuais suportados
- preserva identificadores maiores, evitando trocar nomes como `BaseRepository` por acidente

### O que o script ignora

Por seguranca, ele nao altera conteudo em:

- `.git`
- `.codex`
- `.config`
- `.vs`
- `bin`
- `obj`

### Quando usar `--dry-run` ou `-WhatIf`

Use simulacao quando:

- voce quiser revisar o impacto antes de aplicar
- o repositorio ja tiver alteracoes locais
- voce estiver renomeando pela segunda vez e quiser validar o prefixo antigo

### Quando usar `--code-only` ou `-CodeOnly`

Esse modo limita a troca a arquivos mais tecnicos e evita mexer em parte da documentacao e arquivos textuais gerais. E util quando:

- o repositorio ja foi apresentado externamente com um nome
- voce quer revisar `README`, `DOCKER.md` e materiais textuais manualmente

### Quando usar `--skip-validation` ou `-SkipValidation`

Por padrao o script tenta validar o resultado com build e testes. Pule essa etapa apenas quando:

- voce estiver sem SDK instalado
- estiver rodando em ambiente temporario
- quiser primeiro aplicar o rename e validar depois manualmente

### Recomendacoes antes de rodar

- rode o script com o repositorio limpo ou com commit salvo
- feche Visual Studio, VS Code e terminais apontando para pastas internas do projeto
- prefira testar primeiro com `--dry-run` ou `-WhatIf`
- depois do rename, revise `.env.example`, `README.md`, `docker-compose*.yml` e `launchSettings.json`

## Como criar um novo modulo

Um modulo novo deve seguir o mesmo fluxo de camadas. Pense em modulo como um conjunto coeso de endpoint + caso de uso + persistencia + testes.

Exemplo: modulo `Projects`.

### Passo 1: modelar o dominio

Crie ou ajuste entidades e contratos em `Base.Domain`.

Arquivos tipicos:

- `Base.Domain/Entities/Project.cs`
- `Base.Domain/Interfaces/Repositories/IProjectRepository.cs`

Regras:

- coloque apenas propriedades e relacoes de negocio
- se o dado pertence a uma company, inclua `CompanyId`
- nao coloque DTO de API aqui
- nao coloque codigo de EF Core aqui

### Passo 2: implementar persistencia

Implemente a persistencia em `Base.Infrastructure`.

Arquivos tipicos:

- `Base.Infrastructure/Repositories/ProjectRepository.cs`
- ajuste em [ApplicationDbContext.cs](/C:/dev/Base.API/Base.Infrastructure/Data/ApplicationDbContext.cs)
- ajuste em [UnitOfWork.cs](/C:/dev/Base.API/Base.Infrastructure/UnitOfWork.cs)

Checklist:

- adicionar `DbSet<Project>`
- mapear a entidade no `OnModelCreating`
- criar indices e chaves
- garantir `CompanyId` quando for entidade tenant-scoped
- expor o repository via `IUnitOfWork`

### Passo 3: criar o caso de uso

Implemente o fluxo da aplicacao em `Base.Application`.

Arquivos tipicos:

- `Base.Application/Interfaces/Services/IProjectService.cs`
- `Base.Application/Services/ProjectService.cs`

Essa camada deve:

- validar fluxo
- consultar repositories
- coordenar regras de escrita e leitura
- chamar hashing, token, email ou outros servicos tecnicos quando necessario

Essa camada nao deve:

- conhecer `HttpContext`
- retornar `IActionResult`
- depender de DTOs do controller

### Passo 4: expor na API

Crie controller e DTOs em `Base.API`.

Arquivos tipicos:

- `Base.API/Controllers/ProjectController.cs`
- `Base.API/DTOs/Project.cs`

O controller deve:

- validar entrada HTTP
- traduzir request DTO para parametros simples
- chamar `IProjectService`
- traduzir resposta para DTO
- aplicar autorizacao e contexto de company quando necessario

Se o modulo exigir o tenant atual:

- leia `CompanyId`, `CompanyPublicId` e `UserRole` do `HttpContext.Items`
- siga o mesmo padrao usado em [CompanyController.cs](/C:/dev/Base.API/Base.API/Controllers/CompanyController.cs)

### Passo 5: registrar dependencias

Registre o service no bootstrap.

Arquivo:

- [ServiceCollectionExtensions.cs](/C:/dev/Base.API/Base.API/Extensions/ServiceCollectionExtensions.cs)

Exemplo de registro:

```csharp
services.AddScoped<IProjectService, ProjectService>();
```

Se houver configuracao nova:

- adicione em `appsettings.json`
- adicione em `.env.example` quando fizer sentido

### Passo 6: criar migration

Se o modelo EF mudou, gere migration nova em `Base.Infrastructure/Migrations`.

Comandos uteis:

```bash
dotnet build Base.Infrastructure/Base.Infrastructure.csproj -m:1
dotnet ef migrations add AddProjects --project Base.Infrastructure --startup-project Base.Infrastructure
```

No ambiente local atual, o build/restores ficaram mais estaveis com execucao serial (`-m:1`).

### Passo 7: testar

Crie testes em `Base.Tests`.

Arquivos tipicos:

- `Base.Tests/ProjectControllerTests.cs`
- `Base.Tests/ProjectServiceTests.cs`

Cubra pelo menos:

- fluxo feliz
- autorizacao
- comportamento tenant-aware
- validacoes principais
- regressao de casos criticos

## Template mental para modulo novo

Use esta sequencia:

1. `Domain`: o que existe?
2. `Infrastructure`: como persiste?
3. `Application`: qual o caso de uso?
4. `API`: como expor por HTTP?
5. `Tests`: como proteger o comportamento?
6. `Migrations`: o banco precisa mudar?

Se voce sentir vontade de pular direto para controller + EF no mesmo arquivo, esta saindo do padrao da base.

## Checklist rapido de arquitetura

Antes de fechar um modulo novo:

- o controller so fala com interfaces da `Base.Application`
- a `Base.Application` nao depende de `HttpContext` nem de DTO HTTP
- a `Base.Domain` nao conhece EF Core
- a `Base.Infrastructure` implementa repositories e `UnitOfWork`
- a entidade tenant-aware possui `CompanyId`
- o service foi registrado em DI
- ha testes para o fluxo principal
- se o banco mudou, existe migration nova

## Endpoints

### Auth

- `POST /api/auth/signup`
- `POST /api/auth/login`
- `POST /api/auth/login/initiate`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/me`
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`
- `POST /api/auth/first-access/complete`
- `POST /api/auth/first-access/resend`

Observacoes:

- `forgot-password` e `first-access/resend` sempre retornam mensagem generica
- `reset-password` e `first-access/complete` usam OTP de 6 digitos

### Company

- `POST /api/company`
- `GET /api/company/{id}`
- `GET /api/company/me`
- `PUT /api/company/me`
- `POST /api/company/members/invite`
- `PUT /api/company/{id}`
- `DELETE /api/company/{id}`

## Configuracao

Fontes de configuracao recomendadas:

- Desenvolvimento local com `dotnet run`: `appsettings.json`, `appsettings.Development.json`, `dotnet user-secrets` e variaveis de ambiente.
- Docker, VPS e Azure: variaveis de ambiente.
- Azure em producao: opcionalmente App Settings + Key Vault, mantendo as mesmas chaves.

Chaves importantes da aplicacao:

- `ConnectionStrings__DefaultConnection`
- `ConnectionStrings__HangfireConnection`
- `Jwt__SecretKey`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__AccessTokenExpirationMinutes`
- `Jwt__RefreshTokenExpirationDays`
- `Security__PasswordHashing__TimeCost`
- `Security__PasswordHashing__MemoryCost`
- `Security__PasswordHashing__Lanes`
- `Security__PasswordHashing__Threads`
- `Security__PasswordHashing__HashLength`
- `Security__PasswordHashing__SaltLength`
- `Security__PasswordHashing__Pepper`
- `Security__PasswordReset__Pepper`
- `Seed__Enabled`
- `Seed__AdminEmail`
- `Seed__AdminPassword`
- `Seed__AdminName`
- `Seed__CompanyName`
- `Database__ApplyMigrationsOnStartup`
- `Hangfire__Storage`
- `Hangfire__ConnectionString`
- `Logging__Seq__ServerUrl`

Variaveis de infraestrutura do Docker:

- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `POSTGRES_DB`
- `CONNECTIONSTRINGS__DEFAULTCONNECTION` para sobrescrever o banco do container quando necessario

## Execucao local

### Setup rapido local

Se voce quer apenas fazer a API subir no ambiente de desenvolvimento:

1. Copie `.env.example` para `.env`.
2. Ajuste `POSTGRES_USER`, `POSTGRES_PASSWORD` e `POSTGRES_DB`.
3. Suba a infraestrutura com `docker compose -f docker-compose.dev.yml up -d`.
4. Rode `dotnet restore -m:1`.
5. Configure os `user-secrets` da API:

```bash
dotnet user-secrets --project Base.API set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=base_db;Username=base;Password=change_me_strong_password;Ssl Mode=Disable"
dotnet user-secrets --project Base.API set "Jwt:SecretKey" "change_me_super_secret_key_with_32_chars_min"
dotnet user-secrets --project Base.API set "Security:PasswordHashing:Pepper" "change_me_password_pepper"
dotnet user-secrets --project Base.API set "Security:PasswordReset:Pepper" "change_me_reset_pepper"
dotnet user-secrets --project Base.API set "Seed:AdminPassword" "StrongPassword!123"
dotnet user-secrets --project Base.API set "Seed:Enabled" "true"
```

6. Confira o que esta salvo com `dotnet user-secrets --project Base.API list`.
7. Execute `dotnet run --project Base.API`.
8. A API sobe em `http://localhost:5296` e `https://localhost:7279` pelos perfis locais.
9. Em `Development`, o dashboard do Hangfire fica disponivel em `http://localhost:5296/hangfire` e `https://localhost:7279/hangfire`.

### API local + infraestrutura via Docker

1. Copie `.env.example` para `.env`.
2. Ajuste `POSTGRES_USER`, `POSTGRES_PASSWORD` e `POSTGRES_DB` para a infraestrutura local do Docker.
3. Suba apenas a infraestrutura local com `docker compose -f docker-compose.dev.yml up -d`.
4. Rode `dotnet restore -m:1`.
5. Configure os segredos locais da API:

```bash
dotnet user-secrets --project Base.API set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=base_db;Username=base;Password=change_me_strong_password;Ssl Mode=Disable"
dotnet user-secrets --project Base.API set "Jwt:SecretKey" "change_me_super_secret_key_with_32_chars_min"
dotnet user-secrets --project Base.API set "Security:PasswordHashing:Pepper" "change_me_password_pepper"
dotnet user-secrets --project Base.API set "Security:PasswordReset:Pepper" "change_me_reset_pepper"
```

6. Opcionalmente configure `Seed:AdminPassword` e `Seed:Enabled` via `dotnet user-secrets` se quiser popular dados locais.
7. Execute `dotnet run --project Base.API` ou `./run-dev.ps1` / `./run-dev.sh`.
8. Se o HTTPS local reclamar de certificado no `dotnet run`, execute `dotnet dev-certs https --clean` e depois `dotnet dev-certs https --trust`.

Observacoes:

- Chaves opcionais com valor vazio, como `ZeptoMailSettings:Token`, `Fcm:ProjectId` e `Hangfire:ConnectionString`, nao precisam ser cadastradas no `user-secrets`.
- Se voce trocar `POSTGRES_USER`, `POSTGRES_PASSWORD` ou `POSTGRES_DB` em `.env`, atualize tambem `ConnectionStrings:DefaultConnection` no `user-secrets`.
- O `docker-compose.dev.yml` sobe apenas `db`, `redis` e `seq`; a API local roda por `dotnet run`.
- Em `Development`, `Hangfire:Enabled=true` e `Hangfire:UseDashboard=true` em [appsettings.Development.json](/C:/dev/Base.API/Base.API/appsettings.Development.json), entao o dashboard fica em `/hangfire`.

### Com Docker

1. Copie `.env.example` para `.env`.
2. Execute `docker compose up --build -d`.
3. Endpoints:
   - API: `http://localhost:5000`
   - API HTTPS: `https://localhost:5001`
   - Seq: `http://localhost:5342`

Notas de runtime:

- em `Development`, a API roda por `dotnet run` e aplica migrations no startup
- em `Production`, o compose principal sobe `migrator` + `api`
- o `migrator` aplica migrations versionadas por padrao
- fora do Docker, a API usa `dotnet user-secrets` para segredos locais
- em VPS ou Azure, reutilize as mesmas chaves via variaveis de ambiente ou App Settings

## Build e testes

Comandos base:

```bash
dotnet restore -m:1
dotnet build -m:1
dotnet test -m:1
```

Se voce alterar o modelo EF:

```bash
dotnet build Base.Infrastructure/Base.Infrastructure.csproj -m:1
dotnet ef migrations add NomeDaMigration --project Base.Infrastructure --startup-project Base.API
```
