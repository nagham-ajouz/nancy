using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NotificationService.API.Extensions;
using NotificationService.Infrastructure.Persistence;
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
builder.Services.AddNotificationSwagger();
builder.Services.AddNotificationAuthentication(builder.Configuration);
builder.Services.AddNotificationHealthChecks(builder.Configuration);
builder.Services.AddNotificationInfrastructure(builder.Configuration);

// ── Build ─────────────────────────────────────────────────────
var app = builder.Build();

// ── Migrations ────────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await db.Database.MigrateAsync();
    Log.Information("Notification DB migrations applied");
}
catch (Exception ex)
{
    Log.Warning("Migration skipped: {Error}", ex.Message);
}

// ── Pipeline ──────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.Use(async (HttpContext context, RequestDelegate next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                        ?? Guid.NewGuid().ToString();
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        await next(context);
});

app.MapHealthChecks("/health", HealthCheckExtensions.GetHealthResponse("NotificationService"));

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();