FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["FleetService/FleetService.API/FleetService.API.csproj", "FleetService/FleetService.API/"]
COPY ["FleetService/FleetService.Application/FleetService.Application.csproj", "FleetService/FleetService.Application/"]
COPY ["FleetService/FleetService.Domain/FleetService.Domain.csproj", "FleetService/FleetService.Domain/"]
COPY ["FleetService/FleetService.Infrastructure/FleetService.Infrastructure.csproj", "FleetService/FleetService.Infrastructure/"]
COPY ["Shared/Shared.csproj", "Shared/"]

# Restore
RUN dotnet restore "FleetService/FleetService.API/FleetService.API.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/FleetService/FleetService.API"
RUN dotnet build "FleetService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FleetService.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FleetService.API.dll"]