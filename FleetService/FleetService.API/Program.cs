using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using FleetService.Application.Interfaces;
using FleetService.Application.Mapping;
using FleetService.Application.Services;
using FleetService.Infrastructure.Messaging.Consumers;
using FleetService.Infrastructure.Messaging.Publishers;
using FleetService.Infrastructure.Persistence;
using FleetService.Infrastructure.Repositories;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddDbContext<FleetDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Repositories 
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IDriverRepository,  DriverRepository>();

// Application services
builder.Services.AddScoped<VehicleService>();
builder.Services.AddScoped<DriverService>();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();

