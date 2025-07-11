using CVProcessing.Infrastructure.Queue;
using CVProcessing.Infrastructure.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace CVProcessing.Infrastructure.BackgroundServices;

/// <summary>
/// Extensiones para configurar servicios en background
/// </summary>
public static class BackgroundServicesExtensions
{
    /// <summary>
    /// Agregar servicios de procesamiento en background
    /// </summary>
    public static IServiceCollection AddBackgroundProcessing(this IServiceCollection services)
    {
        // Job Queues
        services.AddSingleton<IJobQueue<DocumentProcessingJob>>(provider =>
            new InMemoryJobQueue<DocumentProcessingJob>(capacity: 10000));
        
        services.AddSingleton<IJobQueue<SessionAnalysisJob>>(provider =>
            new InMemoryJobQueue<SessionAnalysisJob>(capacity: 1000));

        // Notification Service
        services.AddScoped<IProcessingNotificationService, ProcessingNotificationService>();

        // Background Services
        services.AddHostedService<DocumentProcessingService>();
        services.AddHostedService<SessionAnalysisService>();
        services.AddHostedService<JobSchedulerService>();

        return services;
    }

    /// <summary>
    /// Agregar SignalR para notificaciones en tiempo real
    /// </summary>
    public static IServiceCollection AddRealTimeNotifications(this IServiceCollection services)
    {
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}