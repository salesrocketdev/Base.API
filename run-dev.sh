#!/usr/bin/env bash
set -euo pipefail

echo "Applying EF migrations..."
dotnet ef database update --project Base.Infrastructure --startup-project Base.API --context ApplicationDbContext

echo "Starting Base API..."
dotnet run --project Base.API

