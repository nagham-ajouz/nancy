# STAGE 1 — BASE (RUNTIME ONLY)
# This image contains ONLY the .NET runtime (no SDK)
# It will be used to RUN the app (final lightweight container)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Set working directory inside container
WORKDIR /app

# The port the app will listen to inside the container
EXPOSE 8080

# STAGE 2 — BUILD (FULL SDK)
# This image contains the full .NET SDK (used to compile the app)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Working directory for source code
WORKDIR /src


# Copy ONLY project files (.csproj)
# This allows Docker to cache dependencies (important for faster rebuilds)
COPY ["TripService/TripService.API/TripService.API.csproj", "TripService/TripService.API/"]
COPY ["TripService/TripService.Application/TripService.Application.csproj", "TripService/TripService.Application/"]
COPY ["TripService/TripService.Domain/TripService.Domain.csproj", "TripService/TripService.Domain/"]
COPY ["TripService/TripService.Infrastructure/TripService.Infrastructure.csproj", "TripService/TripService.Infrastructure/"]
COPY ["Shared/Shared.csproj", "Shared/"]

# Restore NuGet packages (dependencies)
# This step is cached unless .csproj files change
RUN dotnet restore "TripService/TripService.API/TripService.API.csproj"

# Copy ALL source code into container
COPY . .

# Move into API project folder
WORKDIR "/src/TripService/TripService.API"

# Build the application in Release mode
# Output compiled files into /app/build
RUN dotnet build "TripService.API.csproj" -c Release -o /app/build


# STAGE 3 — PUBLISH (OPTIMIZED OUTPUT)
# Uses build stage to create final deployable files
FROM build AS publish

# Publish = compile + optimize + prepare runtime files
# Output goes to /app/publish
RUN dotnet publish "TripService.API.csproj" -c Release -o /app/publish



# STAGE 4 — FINAL (PRODUCTION IMAGE)
# Start from the lightweight runtime image
FROM base AS final

# Working directory in final container
WORKDIR /app

# Copy ONLY published (compiled) files from publish stage
# This excludes source code and SDK → smaller & secure image
COPY --from=publish /app/publish .


# Entry point: command that runs when container starts
# Runs the compiled .NET application
ENTRYPOINT ["dotnet", "TripService.API.dll"]