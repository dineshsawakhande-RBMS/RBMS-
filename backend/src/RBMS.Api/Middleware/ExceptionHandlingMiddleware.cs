using System.Text.Json;
using RBMS.Application.Common.Exceptions;

namespace RBMS.Api.Middleware;

/// <summary>Translates application exceptions into RFC7807-style ProblemDetails JSON.</summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteResponseAsync(context, ex);
        }
    }

    private async Task WriteResponseAsync(HttpContext context, Exception ex)
    {
        var (status, title, errors) = ex switch
        {
            ValidationException ve => (StatusCodes.Status400BadRequest, "Validation failed", ve.Errors),
            NotFoundException => (StatusCodes.Status404NotFound, ex.Message, null),
            ForbiddenAccessException => (StatusCodes.Status403Forbidden, ex.Message, null),
            ConflictException => (StatusCodes.Status409Conflict, ex.Message, null),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null)
        };

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "Unhandled exception");

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = status;

        var payload = new
        {
            type = $"https://httpstatuses.io/{status}",
            title,
            status,
            traceId = context.TraceIdentifier,
            errors = errors as IReadOnlyDictionary<string, string[]>
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload,
            new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));
    }
}
