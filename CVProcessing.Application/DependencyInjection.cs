using CVProcessing.Application.Services;
using CVProcessing.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CVProcessing.Application;

/// <summary>
/// Configuración de inyección de dependencias para Application
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registrar servicios de aplicación
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Servicios de aplicación
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IAnalysisService, AnalysisService>();
        
        return services;
    }
}