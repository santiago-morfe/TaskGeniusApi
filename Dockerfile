# 1️⃣ Fase de construcción
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia solo los archivos necesarios para restore
COPY ["TaskGeniusApi/TaskGeniusApi.csproj", "TaskGeniusApi/"]
RUN dotnet restore "TaskGeniusApi/TaskGeniusApi.csproj"

# Copia todo y construye
COPY . .
RUN dotnet publish -c Release -o /app/publish

# 2️⃣ Fase de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Ejecutar migraciones antes de iniciar la aplicación
ENTRYPOINT ["sh", "-c", "dotnet TaskGeniusApi.dll --migrate"]