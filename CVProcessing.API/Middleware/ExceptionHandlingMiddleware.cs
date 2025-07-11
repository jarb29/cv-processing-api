using System.Net;
using System.Text.Json;

namespace CVProcessing.API.Middleware;

/// <summary>
/// Middleware para manejo global de excepciones
/// </summary>
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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            Code = "INTERNAL_ERROR",
            Message = "An unexpected error occurred",
            TraceId = context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        switch (exception)
        {
            case ArgumentException:
            case InvalidOperationException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Code = "INVALID_REQUEST";
                errorResponse.Message = exception.Message;
                break;

            case FileNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Code = "RESOURCE_NOT_FOUND";
                errorResponse.Message = exception.Message;
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Code = "UNAUTHORIZED";
                errorResponse.Message = "Access denied";
                break;

            case TimeoutException:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                errorResponse.Code = "TIMEOUT";
                errorResponse.Message = "Request timeout";
                break;

            case NotSupportedException:
                response.StatusCode = (int)HttpStatusCode.NotImplemented;
                errorResponse.Code = "NOT_SUPPORTED";
                errorResponse.Message = exception.Message;
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Code = "INTERNAL_ERROR";
                errorResponse.Message = "An internal server error occurred";
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(new { error = errorResponse }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Respuesta de error estandarizada
/// </summary>
public class ErrorResponse
{
    public required string Code { get; set; }
    public required string Message { get; set; }
    public List<ValidationError>? Details { get; set; }
    public required string TraceId { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Error de validación específico
/// </summary>
public class ValidationError
{
    public required string Field { get; set; }
    public required string Message { get; set; }
}
