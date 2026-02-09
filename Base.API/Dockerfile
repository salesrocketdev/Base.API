# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0.102 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ./Base.API/*.csproj ./Base.API/
COPY ./Base.Core/*.csproj ./Base.Core/
COPY ./Base.Domain/*.csproj ./Base.Domain/
COPY ./Base.Infrastructure/*.csproj ./Base.Infrastructure/
RUN dotnet restore "Base.API/Base.API.csproj"

# Copy everything else and publish
COPY . .
RUN dotnet publish "Base.API/Base.API.csproj" -c Release -o /app/publish /p:SuppressTrimAnalysisWarnings=true

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0.2
WORKDIR /app
ENV ASPNETCORE_URLS="http://+:80;https://+:443"
COPY --from=build /app/publish ./

# Expose HTTP and HTTPS ports (compose/config should map them)
EXPOSE 80 443

ENTRYPOINT ["dotnet", "Base.API.dll"]

