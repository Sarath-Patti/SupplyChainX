# Multi-stage production build for ASP.NET Core Web API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files for layer caching
COPY ["backend/SupplyChainX.sln", "backend/"]
COPY ["backend/src/SupplyChainX.Domain/SupplyChainX.Domain.csproj", "backend/src/SupplyChainX.Domain/"]
COPY ["backend/src/SupplyChainX.Application/SupplyChainX.Application.csproj", "backend/src/SupplyChainX.Application/"]
COPY ["backend/src/SupplyChainX.Infrastructure/SupplyChainX.Infrastructure.csproj", "backend/src/SupplyChainX.Infrastructure/"]
COPY ["backend/src/SupplyChainX.Api/SupplyChainX.Api.csproj", "backend/src/SupplyChainX.Api/"]
COPY ["tests/SupplyChainX.UnitTests/SupplyChainX.UnitTests.csproj", "tests/SupplyChainX.UnitTests/"]

RUN dotnet restore "backend/SupplyChainX.sln"

# Copy full source and publish
COPY backend/ backend/
COPY tests/ tests/

RUN dotnet publish "backend/src/SupplyChainX.Api/SupplyChainX.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Production runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SupplyChainX.Api.dll"]
