using System.Security.Claims;
using System.Text.Json;
using FleetService.Application.Interfaces;
using FleetService.Application.Mapping;
using FleetService.Application.Services;
using FleetService.Infrastructure.Cache;
using FleetService.Infrastructure.Messaging.Consumers;
using FleetService.Infrastructure.Messaging.Publishers;
using FleetService.Infrastructure.Persistence;
using FleetService.Infrastructure.Repositories;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}",
        theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName  = "FleetManagement:";
});
Console.WriteLine($">>> Redis connection: {builder.Configuration.GetConnectionString("Redis")}");
// Register cache service
builder.Services.AddScoped<IFleetCacheService, FleetCacheService>();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Add JWT input box to Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddControllers();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName  = "FleetManagement:"; // prefix for all keys
});

builder.Services.AddAutoMapper(typeof(FleetMappingProfile).Assembly);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority            = builder.Configuration["Keycloak:Authority"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer   = true,
            ValidIssuer      = builder.Configuration["Keycloak:Authority"]
        };

        // ← Add this block for SignalR token support
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // SignalR passes the token as a query string: ?access_token=...
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },

            // Keep your existing OnTokenValidated for role extraction
            OnTokenValidated = context =>
            {
                var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;
                if (claimsIdentity == null) return Task.CompletedTask;
                var realmAccessClaim = context.Principal?.FindFirst("realm_access")?.Value;
                if (realmAccessClaim == null) return Task.CompletedTask;
                using var doc = JsonDocument.Parse(realmAccessClaim);
                if (doc.RootElement.TryGetProperty("roles", out var rolesElement))
                {
                    foreach (var role in rolesElement.EnumerateArray())
                    {
                        var roleName = role.GetString();
                        if (!string.IsNullOrEmpty(roleName))
                            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("Default")!,
        name: "postgresql",
        tags: new[] { "database" })
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{builder.Configuration["RabbitMQ:Username"]}:{builder.Configuration["RabbitMQ:Password"]}@{builder.Configuration["RabbitMQ:Host"]}",
        name: "rabbitmq",
        tags: new[] { "messaging" })
    .AddRedis(
        redisConnectionString: builder.Configuration.GetConnectionString("Redis")!,
        name: "redis",
        tags: new[] { "cache" });

builder.Services.AddDbContext<FleetDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Repositories 
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IDriverRepository,  DriverRepository>();



// Application services
builder.Services.AddScoped<VehicleService>();
builder.Services.AddScoped<DriverService>();
builder.Services.AddScoped<DomainEventDispatcher>();
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
builder.Services.AddScoped<IVehicleService, VehicleService>();

builder.Services.AddMassTransit(x =>
{
    // Register consumers
    x.AddConsumer<TripStartedConsumer>();
    x.AddConsumer<TripCompletedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        var host     = builder.Configuration["RabbitMQ:Host"];
        var username = builder.Configuration["RabbitMQ:Username"];
        var password = builder.Configuration["RabbitMQ:Password"];
        
        cfg.Host(host, "/", h =>
        {
            h.Username(username!);
            h.Password(password!);
        });

        // Queue for Fleet to consume Trip events
        cfg.ReceiveEndpoint("fleet-trip-started", e =>
        {
            e.ConfigureConsumer<TripStartedConsumer>(ctx);
        });

        cfg.ReceiveEndpoint("fleet-trip-completed", e =>
        {
            e.ConfigureConsumer<TripCompletedConsumer>(ctx);
        });
    });
});

// Register publisher
builder.Services.AddScoped<IFleetEventPublisher, FleetEventPublisher>();

var app = builder.Build();

// On startup, publish current status of all vehicles and drivers
// so Trip Service cache is populated immediately
using (var scope = app.Services.CreateScope())
{
    var vehicleRepo  = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();
    var driverRepo   = scope.ServiceProvider.GetRequiredService<IDriverRepository>();
    var publisher    = scope.ServiceProvider.GetRequiredService<IFleetEventPublisher>();

    var vehicles = await vehicleRepo.GetAllAsync();
    foreach (var vehicle in vehicles)
    {
        await publisher.PublishVehicleStatusChangedAsync(
            vehicle.Id, vehicle.Status.ToString());
    }

    var drivers = await driverRepo.GetAllAsync();
    foreach (var driver in drivers)
    {
        await publisher.PublishDriverStatusChangedAsync(
            driver.Id, driver.Status.ToString());
    }

    Log.Information("Startup: published {VehicleCount} vehicle and {DriverCount} driver statuses to RabbitMQ",
        vehicles.Count(), drivers.Count());
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status    = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            service   = "FleetService",
            checks    = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status      = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration    = e.Value.Duration.TotalMilliseconds + "ms"
                })
        };
        await context.Response.WriteAsJsonAsync(result);
    }
});

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.Use(async (HttpContext context, RequestDelegate next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                        ?? Guid.NewGuid().ToString();
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next(context);
    }
});

app.UseAuthentication(); 
app.UseAuthorization();
app.MapControllers();
app.Run();

