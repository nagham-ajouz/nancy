using FleetService.API.Extensions;
using FleetService.Application.Interfaces;
using FleetService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ───────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}",
        theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
    .CreateLogger();

builder.Host.UseSerilog();

// ── Services ──────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddFleetSwagger();
builder.Services.AddFleetAuthentication(builder.Configuration);
builder.Services.AddFleetHealthChecks(builder.Configuration);
builder.Services.AddFleetInfrastructure(builder.Configuration);
builder.Services.AddFleetApplication();

// ── Build ─────────────────────────────────────────────────────
var app = builder.Build();

// ── Migrations ────────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FleetDbContext>();
    await db.Database.MigrateAsync();
    Log.Information("Fleet DB migrations applied");
}
catch (Exception ex)
{
    Log.Warning("Migration skipped: {Error}", ex.Message);
}

// ── Startup publishing ────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var vehicleRepo = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();
    var driverRepo  = scope.ServiceProvider.GetRequiredService<IDriverRepository>();
    var publisher   = scope.ServiceProvider.GetRequiredService<IFleetEventPublisher>();

    var vehicles = await vehicleRepo.GetAllAsync();
    foreach (var vehicle in vehicles)
        await publisher.PublishVehicleStatusChangedAsync(vehicle.Id, vehicle.Status.ToString());

    var drivers = await driverRepo.GetAllAsync();
    foreach (var driver in drivers)
        await publisher.PublishDriverStatusChangedAsync(driver.Id, driver.Status.ToString());

    Log.Information("Startup: published {V} vehicles and {D} drivers", 
        vehicles.Count(), drivers.Count());
}
catch (Exception ex)
{
    Log.Warning("Startup publishing skipped: {Error}", ex.Message);
}

// ── Pipeline ──────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
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
        });
    }
});

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.Use(async (HttpContext context, RequestDelegate next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                        ?? Guid.NewGuid().ToString();
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        await next(context);
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();