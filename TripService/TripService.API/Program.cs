using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<TripTrackingHub>("/hubs/trip-tracking");
app.MapControllers();
app.Run();

