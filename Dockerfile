# 1️⃣ Fase de construcción
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Instalar EF Core CLI globalmente
RUN dotnet tool install --global dotnet-ef --version 8.0.*

# 2. Copiar proyecto y restaurar dependencias
COPY ["TaskGeniusApi/TaskGeniusApi.csproj", "TaskGeniusApi/"]
RUN dotnet restore "TaskGeniusApi/TaskGeniusApi.csproj"

# 3. Copiar todo el código
COPY . .

# 4. Añadir dotnet tools al PATH
ENV PATH="${PATH}:/root/.dotnet/tools"

# 5. Ejecutar migraciones durante el build
RUN dotnet ef database update \
    --project TaskGeniusApi/TaskGeniusApi.csproj \
    --startup-project TaskGeniusApi/TaskGeniusApi.csproj

# 6. Publicar aplicación
RUN dotnet publish -c Release -o /app/publish

# 2️⃣ Fase de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TaskGeniusApi.dll"]