using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Shared.Middleware;
using TripService.API.Hubs;
using TripService.Application.Interfaces;
using TripService.Application.Mapping;
using TripService.Infrastructure.Cache;
using TripService.Infrastructure.Persistence;
using TripService.Infrastructure.Repositories;
using TripService.Application.Services;
using TripService.Infrastructure.Messaging.Consumers;
using TripService.Infrastructure.Messaging.Publishers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

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

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName  = "FleetManagement:"; // prefix for all keys
});

builder.Services.AddSignalR();

builder.Services.AddAutoMapper(typeof(TripMappingProfile).Assembly);

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

        // SignalR token support
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
        tags: new[] { "messaging" });

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

builder.Services.AddDbContext<TripDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Repositories
builder.Services.AddScoped<ITripRepository, TripRepository>();

builder.Services.AddScoped<IVehicleAvailabilityCache, VehicleAvailabilityCache>();

builder.Services.AddScoped<DomainEventDispatcher>();
// Application service
builder.Services.AddScoped<TripAppService>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<VehicleStatusChangedConsumer>();
    x.AddConsumer<DriverStatusChangedConsumer>();
    x.AddConsumer<DriverAssignedConsumer>();
    x.AddConsumer<DriverUnassignedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        // Read from appsettings
        var host     = builder.Configuration["RabbitMQ:Host"];
        var username = builder.Configuration["RabbitMQ:Username"];
        var password = builder.Configuration["RabbitMQ:Password"];

        cfg.Host(host, "/", h =>
        {
            h.Username(username!);
            h.Password(password!);
        });

        cfg.ReceiveEndpoint("trip-vehicle-status-changed", e =>
        {
            e.ConfigureConsumer<VehicleStatusChangedConsumer>(ctx);
        });

        cfg.ReceiveEndpoint("trip-driver-status-changed", e =>
        {
            e.ConfigureConsumer<DriverStatusChangedConsumer>(ctx);
        });

        cfg.ReceiveEndpoint("trip-driver-assigned", e =>
        {
            e.ConfigureConsumer<DriverAssignedConsumer>(ctx);
        });

        cfg.ReceiveEndpoint("trip-driver-unassigned", e =>
        {
            e.ConfigureConsumer<DriverUnassignedConsumer>(ctx);
        });
    });
});

// Register publisher
builder.Services.AddScoped<ITripEventPublisher, TripEventPublisher>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStaticFiles();

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

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status    = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            service   = "TripService",
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

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<TripTrackingHub>("/hubs/trip-tracking");

app.MapControllers();
app.Run();
