using Serilog;
using Shared.Middleware;
using TripService.API.Extensions;
using TripService.API.Hubs;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

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
builder.Services.AddTripSwagger();
builder.Services.AddTripAuthentication(builder.Configuration);
builder.Services.AddTripHealthChecks(builder.Configuration);
builder.Services.AddTripInfrastructure(builder.Configuration);
builder.Services.AddTripApplication();

// ── Build ─────────────────────────────────────────────────────
var app = builder.Build();

// ── Pipeline ──────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStaticFiles();

app.Use(async (HttpContext context, RequestDelegate next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                        ?? Guid.NewGuid().ToString();
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        await next(context);
});

app.MapHealthChecks("/health", HealthCheckExtensions.GetHealthResponse("TripService"));

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<TripTrackingHub>("/hubs/trip-tracking");

app.MapControllers();

app.Run();