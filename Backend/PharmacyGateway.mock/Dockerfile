# Build context must be the repository root, e.g.:
#   docker build -f Backend/PharmacyGateway.mock/Dockerfile -t scriptflow-pharmacy-gateway .
# This is required because the project references the Shared.contract class library
# that lives alongside it under Backend/.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Backend/PharmacyGateway.mock/PharmacyGateway.mock.csproj", "Backend/PharmacyGateway.mock/"]
COPY ["Backend/Shared.contract/Shared.contract.csproj", "Backend/Shared.contract/"]
RUN dotnet restore "Backend/PharmacyGateway.mock/PharmacyGateway.mock.csproj"
COPY Backend/PharmacyGateway.mock/ Backend/PharmacyGateway.mock/
COPY Backend/Shared.contract/ Backend/Shared.contract/
WORKDIR /src/Backend/PharmacyGateway.mock
RUN dotnet build "PharmacyGateway.mock.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PharmacyGateway.mock.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PharmacyGateway.mock.dll"]
