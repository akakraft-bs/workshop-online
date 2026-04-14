# =============================================================================
# Build-Stage: .NET SDK
# Kontext-Wurzel: workshop-online/   (docker build -f src/backend/AkaKraft.WebApi/Dockerfile .)
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Nur .csproj-Dateien kopieren – NuGet-Restore wird gecacht solange sich
# die Projektdateien nicht ändern.
COPY src/backend/AkaKraft.Domain/AkaKraft.Domain.csproj \
     src/backend/AkaKraft.Domain/
COPY src/backend/AkaKraft.Application/AkaKraft.Application.csproj \
     src/backend/AkaKraft.Application/
COPY src/backend/AkaKraft.Infrastructure/AkaKraft.Infrastructure.csproj \
     src/backend/AkaKraft.Infrastructure/
COPY src/backend/AkaKraft.WebApi/AkaKraft.WebApi.csproj \
     src/backend/AkaKraft.WebApi/

RUN dotnet restore src/backend/AkaKraft.WebApi/AkaKraft.WebApi.csproj

# Quellcode kopieren und publishen
COPY src/backend/ src/backend/
RUN dotnet publish src/backend/AkaKraft.WebApi/AkaKraft.WebApi.csproj \
    --no-restore \
    -c Release \
    -o /app/publish

# =============================================================================
# Runtime-Stage: nur ASP.NET Runtime (kein SDK → kleines Image)
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Unprivilegierter Nutzer für mehr Sicherheit
RUN addgroup --system --gid 1001 appgroup \
 && adduser  --system --uid 1001 --ingroup appgroup appuser

COPY --from=build --chown=appuser:appgroup /app/publish .

USER appuser

# ASP.NET Core lauscht auf Port 8080 (nicht 80, da kein root)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AkaKraft.WebApi.dll"]
