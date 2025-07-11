using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace CVProcessing.Infrastructure.Logging;

/// <summary>
/// Middleware para logging de requests y responses HTTP
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Guid.NewGuid().ToString();

        // Agregar correlation ID al contexto
        context.Items["CorrelationId"] = correlationId;

        // Log request
        await LogRequestAsync(context, correlationId);

        // Capturar response
        var originalResponseBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Request failed {Method} {Path} - CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);
            throw;
        }
        finally
        {
            stopwatch.Stop();

            // Log response
            await LogResponseAsync(context, correlationId, stopwatch.ElapsedMilliseconds);

            // Copiar response de vuelta al stream original
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBodyStream);
        }
    }

    private async Task LogRequestAsync(HttpContext context, string correlationId)
    {
        var request = context.Request;

        var requestLog = new
        {
            CorrelationId = correlationId,
            Method = request.Method,
            Path = request.Path.Value,
            QueryString = request.QueryString.Value,
            Headers = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            UserAgent = request.Headers["User-Agent"].ToString(),
            RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
            ContentType = request.ContentType,
            ContentLength = request.ContentLength
        };

        _logger.LogInformation("HTTP Request: {@RequestLog}", requestLog);

        // Log request body para POST/PUT (solo si es pequeño)
        if ((request.Method == "POST" || request.Method == "PUT") &&
            request.ContentLength.HasValue &&
            request.ContentLength.Value < 10000 &&
            request.ContentType?.Contains("application/json") == true)
        {
            request.EnableBuffering();
            var requestBody = await ReadStreamAsync(request.Body);
            request.Body.Position = 0;

            if (!string.IsNullOrEmpty(requestBody))
            {
                _logger.LogDebug("Request Body - CorrelationId: {CorrelationId} - Body: {RequestBody}",
                    correlationId, requestBody);
            }
        }
    }

    private async Task LogResponseAsync(HttpContext context, string correlationId, long elapsedMs)
    {
        var response = context.Response;

        var responseLog = new
        {
            CorrelationId = correlationId,
            StatusCode = response.StatusCode,
            ContentType = response.ContentType,
            ContentLength = response.ContentLength,
            ElapsedMilliseconds = elapsedMs,
            Headers = response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
        };

        var logLevel = response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
        _logger.Log(logLevel, "HTTP Response: {@ResponseLog}", responseLog);

        // Log response body para errores (solo si es pequeño)
        if (response.StatusCode >= 400 &&
            response.Body.Length < 10000 &&
            response.ContentType?.Contains("application/json") == true)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await ReadStreamAsync(response.Body);
            response.Body.Seek(0, SeekOrigin.Begin);

            if (!string.IsNullOrEmpty(responseBody))
            {
                _logger.LogWarning("Error Response Body - CorrelationId: {CorrelationId} - Body: {ResponseBody}",
                    correlationId, responseBody);
            }
        }
    }

    private static async Task<string> ReadStreamAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
