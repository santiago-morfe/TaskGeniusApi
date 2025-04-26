# 1️⃣ Fase de construcción
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Instalar dependencias nativas y EF Core CLI
RUN apt-get update \
    && apt-get install -y --no-install-recommends clang zlib1g-dev \
    && dotnet tool install --global dotnet-ef --version 8.0.*

# 2. Copiar proyecto y restaurar dependencias
COPY ["TaskGeniusApi/TaskGeniusApi.csproj", "TaskGeniusApi/"]
RUN dotnet restore "TaskGeniusApi/TaskGeniusApi.csproj"

# 3. Copiar todo el código
COPY . .

# 4. Configurar entorno para herramientas dotnet
ENV PATH="${PATH}:/root/.dotnet/tools"

# 5. Ejecutar migraciones durante el build
RUN dotnet ef database update \
    --project TaskGeniusApi/TaskGeniusApi.csproj \
    --startup-project TaskGeniusApi/TaskGeniusApi.csproj

# 6. Publicar aplicación
RUN dotnet publish "TaskGeniusApi/TaskGeniusApi.csproj" -c Release -o /app/publish

# 2️⃣ Fase de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TaskGeniusApi.dll", "--apply-migrations"]

# Asegúrate de copiar la base de datos (SQLite)
COPY TaskGeniusApi/MiProyecto.db /app/MiProyecto.db
