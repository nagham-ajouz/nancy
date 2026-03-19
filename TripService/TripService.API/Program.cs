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
        
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                // Keycloak JWT has realm_access.roles as a nested JSON object
                // We need to extract each role and add it as a ClaimTypes.Role
                var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;
                if (claimsIdentity == null) return Task.CompletedTask;

                // Find the raw realm_access claim
                var realmAccessClaim = context.Principal?
                    .FindFirst("realm_access")?.Value;

                if (realmAccessClaim == null) return Task.CompletedTask;

                // Parse the JSON and extract roles array
                using var doc = JsonDocument.Parse(realmAccessClaim);
                if (doc.RootElement.TryGetProperty("roles", out var rolesElement))
                {
                    foreach (var role in rolesElement.EnumerateArray())
                    {
                        var roleName = role.GetString();
                        if (!string.IsNullOrEmpty(roleName))
                        {
                            // Add each role as a standard ClaimTypes.Role claim
                            claimsIdentity.AddClaim(
                                new Claim(ClaimTypes.Role, roleName));
                        }
                    }
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

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

// Cache — singleton so availability state persists across requests
// Task 7 will replace this with a proper Redis/RabbitMQ-backed cache
builder.Services.AddSingleton<IVehicleAvailabilityCache, VehicleAvailabilityCache>();
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

app.UseAuthentication();
app.UseAuthorization();
app.MapHub<TripTrackingHub>("/hubs/trip-tracking");
app.MapControllers();
app.Run();

