# Fleet Management Platform

A microservices-based fleet management system built with ASP.NET Core and Domain-Driven Design.

## Architecture
```
┌─────────────────┐     RabbitMQ      ┌─────────────────┐
│   Fleet Service │ ◄────────────────► │   Trip Service  │
│                 │                   │                 │
│ - Vehicles      │                   │ - Trips         │
│ - Drivers       │                   │ - GPS Tracking  │
│ - State machine │                   │ - Availability  │
│                 │                   │   Cache         │
└────────┬────────┘                   └────────┬────────┘
         │                                     │
         ▼                                     ▼
   fleet_db (PostgreSQL)              trip_db (PostgreSQL)
```

### Services
- **Fleet Service** — manages vehicles, drivers, assignments and lifecycle states
- **Trip Service** — manages trips, GPS tracking, communicates with Fleet via RabbitMQ

### Infrastructure
- **PostgreSQL** — separate database per service
- **RabbitMQ** — async event communication between services
- **Redis** — caching for vehicle/driver lists and availability
- **Keycloak** — JWT authentication and role-based authorization
- **SignalR** — real-time GPS tracking

## Prerequisites

- Docker Desktop
- .NET 8 SDK (for local development only)

## Run with Docker (recommended)
```bash
# Clone the repo
git clone https://github.com/nancyajouz23/InMindProject.git
cd InMindProject

# Start everything
docker-compose up --build
```

This starts: PostgreSQL, Redis, RabbitMQ, Keycloak, Fleet Service, Trip Service.

Wait ~60 seconds for all services to be healthy, then access:

| Service | URL |
|---------|-----|
| Fleet Service API | http://localhost:5141/swagger |
| Trip Service API | http://localhost:5242/swagger |
| RabbitMQ Management | http://localhost:15672 (guest/guest) |
| Keycloak Admin | http://localhost:8080 (admin/admin) |

## Run locally (development)

### 1. Start infrastructure
```bash
docker run -d --name postgres  -p 5432:5432 -e POSTGRES_PASSWORD=postgres postgres:15
docker run -d --name redis     -p 6379:6379 redis:7
docker run -d --name rabbitmq  -p 5672:5672 -p 15672:15672 rabbitmq:3-management
docker run -d --name keycloak  -p 8080:8080 -e KEYCLOAK_ADMIN=admin -e KEYCLOAK_ADMIN_PASSWORD=admin quay.io/keycloak/keycloak:24.0.0 start-dev
```

### 2. Apply migrations
```bash
dotnet ef database update \
  --project FleetService/FleetService.Infrastructure \
  --startup-project FleetService/FleetService.API

dotnet ef database update \
  --project TripService/TripService.Infrastructure \
  --startup-project TripService/TripService.API
```

### 3. Run services
```bash
# Terminal 1
cd FleetService/FleetService.API
dotnet run

# Terminal 2
cd TripService/TripService.API
dotnet run
```

## Keycloak Setup

1. Open http://localhost:8080
2. Login with admin/admin
3. Create realm: `fleet-management`
4. Create client: `fleet-client` (with client authentication ON)
5. Create roles: `Admin`, `FleetManager`, `Dispatcher`, `Driver`
6. Create users and assign roles

## Get a JWT token
```bash
curl -X POST http://localhost:8080/realms/fleet-management/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=fleet-client&client_secret=j5AkaXecRcSx2ZRZAqCdjzqofsBsY8Yk&username=admin_user&password=admin"
```

## Roles

| Role | Permissions |
|------|------------|
| Admin | Full access to both services |
| FleetManager | Manage vehicles and drivers, read-only trips |
| Dispatcher | Create and manage trips, read-only fleet |
| Driver | View own trips, send GPS updates |

## Running Tests
```bash
dotnet test tests/FleetService.Tests
dotnet test tests/TripService.Tests
```

## Project Structure
```
InMindProject/
├── FleetService/
│   ├── FleetService.Domain/        # Entities, value objects, domain events
│   ├── FleetService.Application/   # Services, DTOs, interfaces
│   ├── FleetService.Infrastructure/# EF Core, RabbitMQ, Redis
│   └── FleetService.API/           # Controllers, middleware
├── TripService/
│   ├── TripService.Domain/
│   ├── TripService.Application/
│   ├── TripService.Infrastructure/
│   └── TripService.API/
├── Shared/                         # Common base classes, exceptions, value objects
├── tests/
│   ├── FleetService.Tests/
│   └── TripService.Tests/
├── docker-compose.yml
├── init-db.sql
└── README.md
```