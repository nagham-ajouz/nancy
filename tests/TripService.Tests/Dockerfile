FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["tests/TripService.Tests/TripService.Tests.csproj", "tests/TripService.Tests/"]
COPY ["TripService/TripService.Application/TripService.Application.csproj", "TripService/TripService.Application/"]
COPY ["Shared/Shared.csproj", "Shared/"]
COPY ["TripService/TripService.Domain/TripService.Domain.csproj", "TripService/TripService.Domain/"]
RUN dotnet restore "tests/TripService.Tests/TripService.Tests.csproj"
COPY . .
WORKDIR "/src/tests/TripService.Tests"
RUN dotnet build "./TripService.Tests.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TripService.Tests.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TripService.Tests.dll"]
