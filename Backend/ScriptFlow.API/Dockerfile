# Build context must be the repository root, e.g.:
#   docker build -f Backend/ScriptFlow.API/Dockerfile -t scriptflow-api .
# This is required because the project references the Shared.* class libraries
# that live alongside it under Backend/.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Backend/ScriptFlow.API/ScriptFlow.API.csproj", "Backend/ScriptFlow.API/"]
COPY ["Backend/Shared.contract/Shared.contract.csproj", "Backend/Shared.contract/"]
COPY ["Backend/Shared.Events/Shared.Events.csproj", "Backend/Shared.Events/"]
COPY ["Backend/Shared.Infrastructure/Shared.Infrastructure.csproj", "Backend/Shared.Infrastructure/"]
RUN dotnet restore "Backend/ScriptFlow.API/ScriptFlow.API.csproj"
COPY Backend/ScriptFlow.API/ Backend/ScriptFlow.API/
COPY Backend/Shared.contract/ Backend/Shared.contract/
COPY Backend/Shared.Events/ Backend/Shared.Events/
COPY Backend/Shared.Infrastructure/ Backend/Shared.Infrastructure/
WORKDIR /src/Backend/ScriptFlow.API
RUN dotnet build "ScriptFlow.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ScriptFlow.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ScriptFlow.API.dll"]
