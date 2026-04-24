# ===== Stage 1: Build =====
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (layer caching)
COPY ["SCM_System.csproj", "."]
RUN dotnet restore

# Copy everything else and publish
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ===== Stage 2: Runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# SQLite DB will be stored inside the container (demo only)
# For persistent storage, mount a volume at /app

ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "SCM_System.dll"]
