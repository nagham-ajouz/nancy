using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace Shared.Middleware;

// Catches all unhandled exceptions and maps them to consistent JSON error responses
// Instead of getting a 500 with a stack trace, client gets a clean error object
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Pass request to the next middleware/controller
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the exception with full details for debugging
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Map exception type → HTTP status code
        var statusCode = exception switch
        {
            NotFoundException    => HttpStatusCode.NotFound,           // 404
            DomainException      => HttpStatusCode.BadRequest,         // 400
            ArgumentException    => HttpStatusCode.BadRequest,         // 400
            UnauthorizedAccessException => HttpStatusCode.Unauthorized, // 401
            _                    => HttpStatusCode.InternalServerError  // 500
        };

        // Consistent error response shape for all errors
        var response = new ErrorResponse
        {
            StatusCode = (int)statusCode,
            Message    = exception.Message,
            Type       = exception.GetType().Name
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)statusCode;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}