using CVProcessing.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CVProcessing.API.Controllers;

/// <summary>
/// Controlador para health checks del sistema
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly IOpenAIService _openAIService;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IOpenAIService openAIService,
        IFileStorage fileStorage,
        ILogger<HealthController> logger)
    {
        _openAIService = openAIService;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    /// <summary>
    /// Health check general del sistema
    /// </summary>
    /// <returns>Estado de salud de todos los componentes</returns>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> GetHealth()
    {
        var stopwatch = Stopwatch.StartNew();
        var checks = new Dictionary<string, object>();
        var overallHealthy = true;

        // Check API básico
        checks["api"] = new { status = "Healthy", timestamp = DateTime.UtcNow };

        // Check OpenAI
        try
        {
            var openAIHealthy = await _openAIService.HealthCheckAsync();
            checks["openai"] = new 
            { 
                status = openAIHealthy ? "Healthy" : "Unhealthy",
                timestamp = DateTime.UtcNow
            };
            if (!openAIHealthy) overallHealthy = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI health check failed");
            checks["openai"] = new 
            { 
                status = "Unhealthy", 
                error = ex.Message,
                timestamp = DateTime.UtcNow
            };
            overallHealthy = false;
        }

        // Check Storage
        try
        {
            await _fileStorage.CreateDirectoryAsync("health-check");
            var testPath = "health-check/test.txt";
            await _fileStorage.SaveTextAsync(testPath, "health check test");
            var exists = await _fileStorage.ExistsAsync(testPath);
            await _fileStorage.DeleteFileAsync(testPath);
            
            checks["storage"] = new 
            { 
                status = exists ? "Healthy" : "Unhealthy",
                timestamp = DateTime.UtcNow
            };
            if (!exists) overallHealthy = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Storage health check failed");
            checks["storage"] = new 
            { 
                status = "Unhealthy", 
                error = ex.Message,
                timestamp = DateTime.UtcNow
            };
            overallHealthy = false;
        }

        stopwatch.Stop();

        var response = new
        {
            status = overallHealthy ? "Healthy" : "Unhealthy",
            checks = checks,
            duration = stopwatch.Elapsed.ToString(@"hh\:mm\:ss\.fff"),
            timestamp = DateTime.UtcNow,
            version = GetType().Assembly.GetName().Version?.ToString() ?? "unknown"
        };

        return overallHealthy ? Ok(response) : StatusCode(503, response);
    }

    /// <summary>
    /// Health check simple para load balancers
    /// </summary>
    /// <returns>OK si el servicio está funcionando</returns>
    [HttpGet("ping")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult Ping()
    {
        return Ok(new 
        { 
            status = "OK", 
            timestamp = DateTime.UtcNow,
            uptime = Environment.TickCount64
        });
    }

    /// <summary>
    /// Información detallada del sistema
    /// </summary>
    /// <returns>Información del sistema y configuración</returns>
    [HttpGet("info")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult GetInfo()
    {
        var assembly = GetType().Assembly;
        var version = assembly.GetName().Version;
        
        return Ok(new
        {
            application = new
            {
                name = "CV Processing System",
                version = version?.ToString() ?? "unknown",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown",
                buildDate = System.IO.File.GetCreationTime(assembly.Location)
            },
            system = new
            {
                machineName = Environment.MachineName,
                osVersion = Environment.OSVersion.ToString(),
                processorCount = Environment.ProcessorCount,
                workingSet = Environment.WorkingSet,
                uptime = TimeSpan.FromMilliseconds(Environment.TickCount64).ToString(@"dd\.hh\:mm\:ss")
            },
            runtime = new
            {
                version = Environment.Version.ToString(),
                framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString()
            }
        });
    }

    /// <summary>
    /// Estadísticas de uso de OpenAI
    /// </summary>
    /// <returns>Estadísticas de uso de la API de OpenAI</returns>
    [HttpGet("openai-usage")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> GetOpenAIUsage()
    {
        try
        {
            var usage = await _openAIService.GetUsageStatsAsync();
            return Ok(new
            {
                tokensUsed = usage.TokensUsed,
                tokenLimit = usage.TokenLimit,
                requestCount = usage.RequestCount,
                estimatedCost = usage.EstimatedCost,
                lastReset = usage.LastReset,
                utilizationPercentage = usage.TokenLimit > 0 
                    ? (double)usage.TokensUsed / usage.TokenLimit * 100 
                    : 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get OpenAI usage stats");
            return StatusCode(503, new { error = "Unable to retrieve OpenAI usage statistics" });
        }
    }
}