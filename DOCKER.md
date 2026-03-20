# Docker setup

## Overview
- `docker-compose.dev.yml` defines local infrastructure only: `db`, `redis`, and `seq`.
- `docker-compose.yml` defines the containerized runtime flow: `db`, `redis`, `seq`, `migrator`, and `api`.
- Fora do Docker, a API pode ser executada por `dotnet run --project Base.API`, carregando `.env` automaticamente e derivando a connection string a partir de `POSTGRES_*`.
- Use `.env.example` as reference for runtime variables.

## Local development
1. Copy `.env.example` to `.env` and adjust values.
2. Run `docker compose -f docker-compose.dev.yml up -d`.
3. Start the API with `dotnet run --project Base.API`.
4. Seq UI: `http://localhost:5342`.

## Containerized runtime
1. Copy `.env.example` to `.env` and adjust values.
2. Run `docker compose up --build -d`.
3. API: `http://localhost:5000`.
4. Seq UI: `http://localhost:5342`.

## Notes
- Local database connection uses `Ssl Mode=Disable`.
- For production, enable TLS, externalize secrets, and run behind a reverse proxy.
- In the main compose flow, migrations run via `scripts/ef-migrate.sh` before API startup.
- In the main compose flow, `Seed__Enabled` is forced to `false`.
- In local development, migrations are applied by the API itself on startup.
- Set `EF_VALIDATE_PENDING_MODEL_CHANGES=true` when you want the migrator to fail on EF model drift before applying migrations.
- PostgreSQL only applies `POSTGRES_USER`, `POSTGRES_PASSWORD`, and `POSTGRES_DB` when the data directory is created for the first time. If you change these values later in `.env`, recreate the `db-data` volume or manually alter the database user password inside the existing container.
