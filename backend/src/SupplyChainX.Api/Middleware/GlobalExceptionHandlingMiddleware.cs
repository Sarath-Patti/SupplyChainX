using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChainX.Domain.Exceptions;

namespace SupplyChainX.Api.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during request processing. Path: {Path}, Method: {Method}",
                context.Request.Path, context.Request.Method);

            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var isDbConstraintViolation = exception is DbUpdateException dbUpdateEx &&
            (dbUpdateEx.InnerException?.Message.Contains("foreign key", StringComparison.OrdinalIgnoreCase) == true ||
             dbUpdateEx.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true ||
             dbUpdateEx.InnerException?.Message.Contains("violates", StringComparison.OrdinalIgnoreCase) == true ||
             dbUpdateEx.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true);

        var (statusCode, title, type) = exception switch
        {
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                "https://tools.ietf.org/html/rfc7235#section-3.1"
            ),
            DomainException => (
                HttpStatusCode.BadRequest,
                "Domain Rule Violation",
                "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            ),
            NotFoundException => (
                HttpStatusCode.NotFound,
                "Resource Not Found",
                "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            ),
            ConflictException => (
                HttpStatusCode.Conflict,
                "Resource Conflict",
                "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            ),
            DbUpdateConcurrencyException => (
                HttpStatusCode.Conflict,
                "Concurrent Modification Conflict",
                "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            ),
            DbUpdateException when isDbConstraintViolation => (
                HttpStatusCode.Conflict,
                "Database Constraint Conflict",
                "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred while processing your request.",
                "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            )
        };

        context.Response.StatusCode = (int)statusCode;

        var detailMessage = exception switch
        {
            UnauthorizedAccessException uae => uae.Message,
            DomainException de => de.Message,
            NotFoundException nfe => nfe.Message,
            ConflictException ce => ce.Message,
            DbUpdateConcurrencyException => "The requested resource was updated or modified concurrently by another transaction. Please retry your request.",
            DbUpdateException when isDbConstraintViolation => "The request could not be completed due to a database constraint violation.",
            _ => _env.IsDevelopment() ? exception.Message : "A server error occurred. Please contact support if the issue persists."
        };

        var correlationId = context.Response.Headers["X-Correlation-ID"].FirstOrDefault() ?? context.TraceIdentifier;

        var problemDetails = new ProblemDetails
        {
            Status = context.Response.StatusCode,
            Title = title,
            Type = type,
            Detail = detailMessage,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["correlationId"] = correlationId;

        if (_env.IsDevelopment() && statusCode == HttpStatusCode.InternalServerError)
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(problemDetails, options);
        return context.Response.WriteAsync(json);
    }
}
