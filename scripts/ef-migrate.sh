#!/usr/bin/env bash
set -euo pipefail

cd /src

dotnet tool restore
dotnet restore Base.Boilerplate.sln -v minimal

# Optional guard for CI or local validation. Runtime migration should only apply committed migrations.
if [[ "${EF_VALIDATE_PENDING_MODEL_CHANGES:-false}" == "true" ]]; then
  dotnet tool run dotnet-ef -- migrations has-pending-model-changes --project Base.Infrastructure --startup-project Base.API
fi

dotnet tool run dotnet-ef -- database update --project Base.Infrastructure --startup-project Base.API
