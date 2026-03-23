FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["tests/FleetService.Tests/FleetService.Tests.csproj", "tests/FleetService.Tests/"]
COPY ["FleetService/FleetService.Application/FleetService.Application.csproj", "FleetService/FleetService.Application/"]
COPY ["FleetService/FleetService.Domain/FleetService.Domain.csproj", "FleetService/FleetService.Domain/"]
COPY ["Shared/Shared.csproj", "Shared/"]
COPY ["FleetService/FleetService.Infrastructure/FleetService.Infrastructure.csproj", "FleetService/FleetService.Infrastructure/"]
COPY ["FleetService/FleetService.API/FleetService.API.csproj", "FleetService/FleetService.API/"]
RUN dotnet restore "tests/FleetService.Tests/FleetService.Tests.csproj"
COPY . .
WORKDIR "/src/tests/FleetService.Tests"
RUN dotnet build "./FleetService.Tests.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FleetService.Tests.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FleetService.Tests.dll"]
