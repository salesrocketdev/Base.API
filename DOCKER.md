# Docker setup (API + PostgreSQL)

## Overview
- `docker-compose.yml` defines `db` (PostgreSQL), `migrator` (EF migrations), `api` (Base API), and `seq` (log server).
- Use `.env.example` as reference for runtime variables.

## Quick start
1. Copy `.env.example` to `.env` and adjust values.
2. Run `docker compose up --build -d`.
3. API: `http://localhost:5000`.
4. Seq UI: `http://localhost:5342`.

## Notes
- Local database connection uses `Ssl Mode=Disable`.
- For production, enable TLS, externalize secrets, and run behind a reverse proxy.
- Migrations run via `scripts/ef-migrate.sh` before API startup.
