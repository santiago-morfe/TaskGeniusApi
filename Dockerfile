# 1️⃣ Fase de construcción con SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia solo el proyecto y restaura dependencias
COPY ["TaskGeniusApi/TaskGeniusApi.csproj", "TaskGeniusApi/"]
RUN dotnet restore "TaskGeniusApi/TaskGeniusApi.csproj"

# Copia todo el código y publica
COPY . .
RUN dotnet publish -c Release -o /app/publish

# 🔄 Ejecuta migraciones durante el build (requiere EF Core CLI)
RUN dotnet ef database update --project TaskGeniusApi/TaskGeniusApi.csproj --startup-project TaskGeniusApi/TaskGeniusApi.csproj

# 2️⃣ Fase de ejecución ligera
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Configura variables de entorno para migraciones automáticas
ENV ASPNETCORE_ENVIRONMENT=Production
ENV RUN_MIGRATIONS_ON_STARTUP=true

COPY --from=build /app/publish .

# 🐳 Entrypoint optimizado para migraciones + ejecución
ENTRYPOINT ["sh", "-c", "if [ \"$RUN_MIGRATIONS_ON_STARTUP\" = \"true\" ]; then dotnet TaskGeniusApi.dll --migrate; fi && dotnet TaskGeniusApi.dll"]