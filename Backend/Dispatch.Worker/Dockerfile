# Build context must be the repository root, e.g.:
#   docker build -f Backend/Dispatch.Worker/Dockerfile -t scriptflow-dispatch-worker .
# This is required because the project references the Shared.* class libraries
# that live alongside it under Backend/.
#
# This is a plain Worker Service (no Kestrel/HTTP endpoint), so unlike the API
# projects there is no EXPOSE / ASPNETCORE_URLS here.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Backend/Dispatch.Worker/Dispatch.Worker.csproj", "Backend/Dispatch.Worker/"]
COPY ["Backend/Shared.contract/Shared.contract.csproj", "Backend/Shared.contract/"]
COPY ["Backend/Shared.Events/Shared.Events.csproj", "Backend/Shared.Events/"]
COPY ["Backend/Shared.Infrastructure/Shared.Infrastructure.csproj", "Backend/Shared.Infrastructure/"]
RUN dotnet restore "Backend/Dispatch.Worker/Dispatch.Worker.csproj"
COPY Backend/Dispatch.Worker/ Backend/Dispatch.Worker/
COPY Backend/Shared.contract/ Backend/Shared.contract/
COPY Backend/Shared.Events/ Backend/Shared.Events/
COPY Backend/Shared.Infrastructure/ Backend/Shared.Infrastructure/
WORKDIR /src/Backend/Dispatch.Worker
RUN dotnet build "Dispatch.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Dispatch.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Dispatch.Worker.dll"]
