#!/usr/bin/env bash
set -euo pipefail

cd /src

dotnet tool restore
dotnet restore Base.Boilerplate.sln -v minimal

# Fail fast when the model has pending changes instead of mutating source code in runtime.
dotnet tool run dotnet-ef -- migrations has-pending-model-changes --project Base.Infrastructure --startup-project Base.API

dotnet tool run dotnet-ef -- database update --project Base.Infrastructure --startup-project Base.API

