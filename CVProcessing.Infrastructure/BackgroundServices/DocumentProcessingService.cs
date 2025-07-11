using CVProcessing.Core.Enums;
using CVProcessing.Core.Interfaces;
using CVProcessing.Infrastructure.Queue;
using CVProcessing.Infrastructure.SignalR;
using CVProcessing.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CVProcessing.Infrastructure.BackgroundServices;

/// <summary>
/// Servicio en background para procesamiento de documentos
/// </summary>
public class DocumentProcessingService : BackgroundService
{
    private readonly IJobQueue<DocumentProcessingJob> _jobQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<DocumentProcessingService> _logger;
    private readonly int _maxConcurrency;

    public DocumentProcessingService(
        IJobQueue<DocumentProcessingJob> jobQueue,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<DocumentProcessingService> logger)
    {
        _jobQueue = jobQueue;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _maxConcurrency = Environment.ProcessorCount; // Usar número de cores disponibles
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Document Processing Service started with {MaxConcurrency} concurrent workers", _maxConcurrency);

        // Crear múltiples workers para procesamiento concurrente
        var workers = Enumerable.Range(0, _maxConcurrency)
            .Select(i => ProcessDocumentsAsync($"Worker-{i}", stoppingToken))
            .ToArray();

        await Task.WhenAll(workers);
    }

    private async Task ProcessDocumentsAsync(string workerName, CancellationToken stoppingToken)
    {
        _logger.LogDebug("{WorkerName} started", workerName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _jobQueue.DequeueAsync(stoppingToken);
                if (job == null) continue;

                _logger.LogInformation("{WorkerName} processing document {DocumentId} from session {SessionId}",
                    workerName, job.DocumentId, job.SessionId);

                await ProcessDocumentJob(job, workerName);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{WorkerName} encountered an error", workerName);
                await Task.Delay(1000, stoppingToken); // Brief delay before retrying
            }
        }

        _logger.LogDebug("{WorkerName} stopped", workerName);
    }

    private async Task ProcessDocumentJob(DocumentProcessingJob job, string workerName)
    {
        using var mainScope = _serviceScopeFactory.CreateScope();
        var documentService = mainScope.ServiceProvider.GetRequiredService<IDocumentService>();
        var sessionService = mainScope.ServiceProvider.GetRequiredService<ISessionService>();
        var sessionRepository = mainScope.ServiceProvider.GetRequiredService<SessionRepository>();

        try
        {
            // Obtener sesión y documento
            var session = await sessionRepository.GetByIdAsync(job.SessionId);
            if (session == null)
            {
                _logger.LogWarning("{WorkerName} - Session {SessionId} not found", workerName, job.SessionId);
                return;
            }

            var document = session.Documents.FirstOrDefault(d => d.Id == job.DocumentId);
            if (document == null)
            {
                _logger.LogWarning("{WorkerName} - Document {DocumentId} not found in session {SessionId}",
                    workerName, job.DocumentId, job.SessionId);
                return;
            }

            // Actualizar estado del documento
            document.Status = DocumentStatus.Extracting;
            await sessionRepository.UpdateAsync(session);

        // Notificar inicio de procesamiento
        using (var notificationScope1 = _serviceScopeFactory.CreateScope())
        {
            var notificationService = notificationScope1.ServiceProvider.GetRequiredService<IProcessingNotificationService>();
            await notificationService.NotifyDocumentProcessedAsync(job.SessionId, job.DocumentId, false, "Processing started");
        }

            // Procesar documento
            var startTime = DateTime.UtcNow;
            var cvData = await documentService.ProcessAsync(job.DocumentId, session.JobOffer);
            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // Actualizar documento con resultado
            document.ExtractedData = cvData;
            document.Status = DocumentStatus.Processed;
            document.ProcessedAt = DateTime.UtcNow;
            document.ProcessingTimeMs = (long)processingTime;

            await sessionRepository.UpdateAsync(session);

            // Notificar éxito
            using (var notificationScope2 = _serviceScopeFactory.CreateScope())
            {
                var notificationService = notificationScope2.ServiceProvider.GetRequiredService<IProcessingNotificationService>();
                await notificationService.NotifyDocumentProcessedAsync(job.SessionId, job.DocumentId, true);
            }

            // Actualizar progreso de la sesión
            await UpdateSessionProgress(session, sessionService);

            _logger.LogInformation("{WorkerName} successfully processed document {DocumentId} in {ProcessingTime}ms",
                workerName, job.DocumentId, processingTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{WorkerName} failed to process document {DocumentId}", workerName, job.DocumentId);

            // Actualizar documento con error
            try
            {
                var session = await sessionRepository.GetByIdAsync(job.SessionId);
                if (session != null)
                {
                    var document = session.Documents.FirstOrDefault(d => d.Id == job.DocumentId);
                    if (document != null)
                    {
                        document.Status = DocumentStatus.Failed;
                        document.ErrorMessage = ex.Message;
                        await sessionRepository.UpdateAsync(session);
                    }
                }
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "{WorkerName} failed to update document status after error", workerName);
            }

            // Notificar error
            using (var notificationScope3 = _serviceScopeFactory.CreateScope())
            {
                var notificationService = notificationScope3.ServiceProvider.GetRequiredService<IProcessingNotificationService>();
                await notificationService.NotifyDocumentProcessedAsync(job.SessionId, job.DocumentId, false, ex.Message);
            }
        }
    }

    private async Task UpdateSessionProgress(Core.Entities.Session session, ISessionService sessionService)
    {
        var totalDocuments = session.Documents.Count;
        var processedDocuments = session.Documents.Count(d =>
            d.Status == DocumentStatus.Processed || d.Status == DocumentStatus.Failed);

        if (totalDocuments == 0) return;

        var progress = (int)((double)processedDocuments / totalDocuments * 100);

        await sessionService.UpdateProgressAsync(session.Id, progress);
        using (var notificationScope4 = _serviceScopeFactory.CreateScope())
        {
            var notificationService = notificationScope4.ServiceProvider.GetRequiredService<IProcessingNotificationService>();
            await notificationService.NotifySessionProgressAsync(session.Id, progress,
                $"Processed {processedDocuments}/{totalDocuments} documents");
        }

        // Si todos los documentos están procesados, cambiar estado de sesión
        if (processedDocuments == totalDocuments)
        {
            var hasFailures = session.Documents.Any(d => d.Status == DocumentStatus.Failed);
            var newStatus = hasFailures ? SessionStatus.Completed : SessionStatus.Completed;

            await sessionService.UpdateStatusAsync(session.Id, newStatus,
                $"Processing completed. {session.Documents.Count(d => d.Status == DocumentStatus.Processed)} successful, {session.Documents.Count(d => d.Status == DocumentStatus.Failed)} failed");

            using (var notificationScope5 = _serviceScopeFactory.CreateScope())
            {
                var notificationService = notificationScope5.ServiceProvider.GetRequiredService<IProcessingNotificationService>();
                await notificationService.NotifySessionStatusChangedAsync(session.Id, newStatus.ToString(),
                    "All documents processed");
            }

            _logger.LogInformation("Session {SessionId} processing completed", session.Id);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Document Processing Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}
