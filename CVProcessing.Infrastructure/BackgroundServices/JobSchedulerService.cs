using CVProcessing.Core.Enums;
using CVProcessing.Infrastructure.Queue;
using CVProcessing.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CVProcessing.Infrastructure.BackgroundServices;

/// <summary>
/// Servicio para programar trabajos automáticamente
/// </summary>
public class JobSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IJobQueue<DocumentProcessingJob> _documentQueue;
    private readonly IJobQueue<SessionAnalysisJob> _analysisQueue;
    private readonly ILogger<JobSchedulerService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public JobSchedulerService(
        IServiceScopeFactory serviceScopeFactory,
        IJobQueue<DocumentProcessingJob> documentQueue,
        IJobQueue<SessionAnalysisJob> analysisQueue,
        ILogger<JobSchedulerService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _documentQueue = documentQueue;
        _analysisQueue = analysisQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job Scheduler Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SchedulePendingJobs();
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Job Scheduler Service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Job Scheduler Service stopped");
    }

    private async Task SchedulePendingJobs()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<SessionRepository>();

        try
        {
            var sessions = await sessionRepository.GetAllAsync();

            foreach (var session in sessions)
            {
                // Programar procesamiento de documentos pendientes
                await ScheduleDocumentProcessing(session);

                // Programar análisis de sesiones completadas
                await ScheduleSessionAnalysis(session);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling pending jobs");
        }
    }

    private async Task ScheduleDocumentProcessing(Core.Entities.Session session)
    {
        var pendingDocuments = session.Documents
            .Where(d => d.Status == DocumentStatus.Uploaded)
            .ToList();

        if (!pendingDocuments.Any()) return;

        _logger.LogDebug("Scheduling {Count} documents for processing in session {SessionId}",
            pendingDocuments.Count, session.Id);

        foreach (var document in pendingDocuments)
        {
            var job = new DocumentProcessingJob
            {
                SessionId = session.Id,
                DocumentId = document.Id,
                DocumentPath = document.FilePath,
                Priority = CalculateDocumentPriority(document, session)
            };

            await _documentQueue.EnqueueAsync(job);

            // Actualizar estado del documento para evitar reprogramación
            document.Status = DocumentStatus.Extracting;
        }

        if (pendingDocuments.Any())
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var sessionRepository = scope.ServiceProvider.GetRequiredService<SessionRepository>();
            await sessionRepository.UpdateAsync(session);
        }
    }

    private async Task ScheduleSessionAnalysis(Core.Entities.Session session)
    {
        // Solo analizar sesiones que tienen todos los documentos procesados
        var totalDocuments = session.Documents.Count;
        var processedDocuments = session.Documents.Count(d => d.Status == DocumentStatus.Processed);
        var failedDocuments = session.Documents.Count(d => d.Status == DocumentStatus.Failed);

        // Verificar si todos los documentos están procesados (exitosos o fallidos)
        if (totalDocuments == 0 || (processedDocuments + failedDocuments) < totalDocuments)
            return;

        // Verificar si ya tiene matriz de comparación
        if (session.ComparisonMatrix != null)
            return;

        // Verificar si hay al menos un documento procesado exitosamente
        if (processedDocuments == 0)
        {
            _logger.LogWarning("Session {SessionId} has no successfully processed documents for analysis", session.Id);
            return;
        }

        _logger.LogDebug("Scheduling analysis for session {SessionId} with {ProcessedCount} processed documents",
            session.Id, processedDocuments);

        var analysisJob = new SessionAnalysisJob
        {
            SessionId = session.Id,
            GenerateMatrix = true,
            GenerateRecommendations = true
        };

        await _analysisQueue.EnqueueAsync(analysisJob);
    }

    private static int CalculateDocumentPriority(Core.Entities.Document document, Core.Entities.Session session)
    {
        var priority = 0;

        // Prioridad basada en el tiempo de subida (más antiguos primero)
        var hoursOld = (DateTime.UtcNow - document.UploadedAt).TotalHours;
        priority += (int)Math.Min(hoursOld, 24); // Max 24 puntos por antigüedad

        // Prioridad basada en el tamaño del archivo (archivos más pequeños primero)
        if (document.FileSize < 1024 * 1024) // < 1MB
            priority += 10;
        else if (document.FileSize < 5 * 1024 * 1024) // < 5MB
            priority += 5;

        // Prioridad basada en el número total de documentos en la sesión
        if (session.Documents.Count <= 5)
            priority += 15; // Sesiones pequeñas tienen prioridad
        else if (session.Documents.Count <= 20)
            priority += 10;
        else
            priority += 5;

        return priority;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job Scheduler Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}
