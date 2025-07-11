using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CVProcessing.Infrastructure.SignalR;

/// <summary>
/// Implementación del servicio de notificaciones usando SignalR
/// </summary>
public class ProcessingNotificationService : IProcessingNotificationService
{
    private readonly IHubContext<ProcessingHub> _hubContext;
    private readonly ILogger<ProcessingNotificationService> _logger;

    public ProcessingNotificationService(
        IHubContext<ProcessingHub> hubContext,
        ILogger<ProcessingNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifySessionProgressAsync(Guid sessionId, int progress, string? message = null)
    {
        _logger.LogDebug("Notifying session progress: {SessionId} - {Progress}%", sessionId, progress);

        await _hubContext.Clients.Group($"session-{sessionId}")
            .SendAsync("ProcessingProgress", new
            {
                sessionId,
                progress,
                message,
                timestamp = DateTime.UtcNow
            });
    }

    public async Task NotifyDocumentProcessedAsync(Guid sessionId, Guid documentId, bool success, string? error = null)
    {
        _logger.LogDebug("Notifying document processed: {DocumentId} - Success: {Success}", documentId, success);

        await _hubContext.Clients.Group($"session-{sessionId}")
            .SendAsync("DocumentProcessed", new
            {
                sessionId,
                documentId,
                success,
                error,
                timestamp = DateTime.UtcNow
            });
    }

    public async Task NotifyAnalysisCompleteAsync(Guid sessionId, object analysisResult)
    {
        _logger.LogInformation("Notifying analysis complete for session: {SessionId}", sessionId);

        await _hubContext.Clients.Group($"session-{sessionId}")
            .SendAsync("AnalysisComplete", new
            {
                sessionId,
                result = analysisResult,
                timestamp = DateTime.UtcNow
            });
    }

    public async Task NotifyProcessingErrorAsync(Guid sessionId, string error)
    {
        _logger.LogWarning("Notifying processing error for session: {SessionId} - {Error}", sessionId, error);

        await _hubContext.Clients.Group($"session-{sessionId}")
            .SendAsync("ProcessingError", new
            {
                sessionId,
                error,
                timestamp = DateTime.UtcNow
            });
    }

    public async Task NotifySessionStatusChangedAsync(Guid sessionId, string status, string? message = null)
    {
        _logger.LogDebug("Notifying session status changed: {SessionId} - {Status}", sessionId, status);

        await _hubContext.Clients.Group($"session-{sessionId}")
            .SendAsync("SessionStatusChanged", new
            {
                sessionId,
                status,
                message,
                timestamp = DateTime.UtcNow
            });
    }
}

/// <summary>
/// SignalR Hub para procesamiento en tiempo real
/// </summary>
public class ProcessingHub : Hub
{
    private readonly ILogger<ProcessingHub> _logger;

    public ProcessingHub(ILogger<ProcessingHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Unirse a un grupo de sesión para recibir notificaciones
    /// </summary>
    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sessionId}");
        _logger.LogDebug("Client {ConnectionId} joined session {SessionId}", Context.ConnectionId, sessionId);
    }

    /// <summary>
    /// Salir de un grupo de sesión
    /// </summary>
    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session-{sessionId}");
        _logger.LogDebug("Client {ConnectionId} left session {SessionId}", Context.ConnectionId, sessionId);
    }

    /// <summary>
    /// Obtener estado de conexión
    /// </summary>
    public async Task GetConnectionInfo()
    {
        await Clients.Caller.SendAsync("ConnectionInfo", new
        {
            connectionId = Context.ConnectionId,
            timestamp = DateTime.UtcNow
        });
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogDebug("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}