namespace CVProcessing.Infrastructure.SignalR;

/// <summary>
/// Servicio para notificaciones en tiempo real del procesamiento
/// </summary>
public interface IProcessingNotificationService
{
    /// <summary>
    /// Notificar progreso de procesamiento de sesión
    /// </summary>
    Task NotifySessionProgressAsync(Guid sessionId, int progress, string? message = null);
    
    /// <summary>
    /// Notificar que un documento ha sido procesado
    /// </summary>
    Task NotifyDocumentProcessedAsync(Guid sessionId, Guid documentId, bool success, string? error = null);
    
    /// <summary>
    /// Notificar que el análisis de sesión ha sido completado
    /// </summary>
    Task NotifyAnalysisCompleteAsync(Guid sessionId, object analysisResult);
    
    /// <summary>
    /// Notificar error en el procesamiento
    /// </summary>
    Task NotifyProcessingErrorAsync(Guid sessionId, string error);
    
    /// <summary>
    /// Notificar cambio de estado de sesión
    /// </summary>
    Task NotifySessionStatusChangedAsync(Guid sessionId, string status, string? message = null);
}