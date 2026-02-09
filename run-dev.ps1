Write-Host "Applying EF migrations..."
dotnet ef database update --project Base.Infrastructure --startup-project Base.API --context ApplicationDbContext

Write-Host "Starting Base API..."
dotnet run --project Base.API

