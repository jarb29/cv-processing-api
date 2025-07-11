using CVProcessing.Core.Interfaces;
using CVProcessing.Infrastructure.BackgroundServices;
using CVProcessing.Infrastructure.Logging;
using CVProcessing.Infrastructure.OpenAI;
using CVProcessing.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CVProcessing.Infrastructure;

/// <summary>
/// Configuración de inyección de dependencias para Infrastructure
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registrar servicios de infraestructura
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuración de OpenAI
        services.Configure<OpenAIConfiguration>(options =>
            configuration.GetSection(OpenAIConfiguration.SectionName).Bind(options));

        // Servicios de almacenamiento
        var storagePath = configuration.GetValue<string>("Storage:Path") ?? "storage";
        services.AddSingleton<IFileStorage>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<LocalFileStorage>>();
            return new LocalFileStorage(logger, storagePath);
        });

        services.AddScoped<SessionRepository>();

        // Servicios de OpenAI
        services.AddScoped<IOpenAIService, OpenAIService>();

        // Logging
        // Do NOT register RequestLoggingMiddleware as a singleton. It is added to the pipeline directly in Program.cs.

        // Background Processing
        services.AddBackgroundProcessing();
        services.AddRealTimeNotifications();

        return services;
    }

    /// <summary>
    /// Configurar Serilog como proveedor de logging
    /// </summary>
    public static IServiceCollection AddSerilogLogging(this IServiceCollection services, IConfiguration configuration)
    {
        var logPath = configuration.GetValue<string>("Logging:Path") ?? "logs";

        var logger = SerilogConfiguration.CreateLogger(logPath);

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(logger);
        });

        return services;
    }
}
