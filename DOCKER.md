# Docker setup

## Overview
- `docker-compose.dev.yml` defines local infrastructure only: `db`, `redis`, and `seq`.
- `docker-compose.yml` defines the containerized runtime flow: `db`, `redis`, `seq`, `migrator`, and `api`.
- Fora do Docker, a API deve ser executada por `dotnet run --project Base.API`, lendo `appsettings`, `appsettings.Development.json`, `dotnet user-secrets` e variaveis de ambiente.
- Use `.env.example` como referencia para `docker compose` e para runtime containerizado.

## Local development
1. Copy `.env.example` to `.env` and adjust only the infrastructure values used by `docker compose`, especially `POSTGRES_*`.
2. Run `docker compose -f docker-compose.dev.yml up -d`.
3. Configure the local API secrets with `dotnet user-secrets --project Base.API set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=base_db;Username=base;Password=change_me_strong_password;Ssl Mode=Disable"`.
4. Configure the remaining local secrets with `dotnet user-secrets --project Base.API set`, for example `Jwt:SecretKey`, `Security:PasswordHashing:Pepper`, and `Security:PasswordReset:Pepper`.
5. Start the API with `dotnet run --project Base.API`.
6. Seq UI: `http://localhost:5342`.

## Containerized runtime
1. Copy `.env.example` to `.env` and adjust values.
2. Run `docker compose up --build -d`.
3. API: `http://localhost:5000`.
4. Seq UI: `http://localhost:5342`.

## Notes
- Local database connection uses `Ssl Mode=Disable`.
- For production, prefer environment variables or a secrets manager such as Azure Key Vault instead of `user-secrets`.
- In the main compose flow, migrations run via `scripts/ef-migrate.sh` before API startup.
- In the main compose flow, `Seed__Enabled` is forced to `false`.
- In local development, migrations are applied by the API itself on startup when `Database:ApplyMigrationsOnStartup=true`.
- Set `EF_VALIDATE_PENDING_MODEL_CHANGES=true` when you want the migrator to fail on EF model drift before applying migrations.
- PostgreSQL only applies `POSTGRES_USER`, `POSTGRES_PASSWORD`, and `POSTGRES_DB` when the data directory is created for the first time. If you change these values later in `.env`, recreate the `db-data` volume or manually alter the database user password inside the existing container.
