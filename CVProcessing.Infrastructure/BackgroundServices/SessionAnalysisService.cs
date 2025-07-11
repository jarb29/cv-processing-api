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
/// Servicio en background para análisis de sesiones
/// </summary>
public class SessionAnalysisService : BackgroundService
{
    private readonly IJobQueue<SessionAnalysisJob> _jobQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SessionAnalysisService> _logger;

    public SessionAnalysisService(
        IJobQueue<SessionAnalysisJob> jobQueue,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<SessionAnalysisService> logger)
    {
        _jobQueue = jobQueue;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session Analysis Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _jobQueue.DequeueAsync(stoppingToken);
                if (job == null) continue;

                _logger.LogInformation("Processing analysis for session {SessionId}", job.SessionId);
                await ProcessAnalysisJob(job);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Session Analysis Service");
                await Task.Delay(5000, stoppingToken); // Wait before retrying
            }
        }

        _logger.LogInformation("Session Analysis Service stopped");
    }

    private async Task ProcessAnalysisJob(SessionAnalysisJob job)
    {
        using var mainScope = _serviceScopeFactory.CreateScope();
        var analysisService = mainScope.ServiceProvider.GetRequiredService<IAnalysisService>();
        var sessionService = mainScope.ServiceProvider.GetRequiredService<ISessionService>();
        var sessionRepository = mainScope.ServiceProvider.GetRequiredService<SessionRepository>();

        try
        {
            // Verificar que la sesión existe y tiene documentos procesados
            var session = await sessionRepository.GetByIdAsync(job.SessionId);
            if (session == null)
            {
                _logger.LogWarning("Session {SessionId} not found for analysis", job.SessionId);
                return;
            }

            var processedDocuments = session.Documents.Where(d => d.Status == DocumentStatus.Processed).ToList();
            if (!processedDocuments.Any())
            {
                _logger.LogWarning("No processed documents found in session {SessionId}", job.SessionId);
                return;
            }

            _logger.LogInformation("Starting analysis for session {SessionId} with {DocumentCount} processed documents", 
                job.SessionId, processedDocuments.Count);

            // Notificar inicio del análisis
        using (var notificationScope1 = _serviceScopeFactory.CreateScope())
        {
            var notificationService = notificationScope1.ServiceProvider.GetRequiredService<IProcessingNotificationService>();
            await notificationService.NotifySessionProgressAsync(job.SessionId, 90, "Starting analysis...");
        }

            var startTime = DateTime.UtcNow;
            var results = new Dictionary<string, object>();

            // Generar matriz de comparación si se solicita
            if (job.GenerateMatrix)
            {
                _logger.LogDebug("Generating comparison matrix for session {SessionId}", job.SessionId);
                var matrix = await analysisService.GenerateComparisonMatrixAsync(job.SessionId);
                results["comparisonMatrix"] = new
                {
                    sessionId = matrix.SessionId,
                    candidatesCount = matrix.Candidates.Count,
                    topCandidate = matrix.Candidates.FirstOrDefault()?.Name,
                    averageScore = matrix.Statistics.AverageScore,
                    generatedAt = matrix.GeneratedAt
                };
            }

            // Generar recomendaciones si se solicita
            if (job.GenerateRecommendations)
            {
                _logger.LogDebug("Generating hiring recommendations for session {SessionId}", job.SessionId);
                var recommendations = await analysisService.GenerateHiringRecommendationsAsync(job.SessionId, 5);
                results["recommendations"] = recommendations.Select(r => new
                {
                    candidateName = r.Candidate.Name,
                    recommendation = r.Recommendation.ToString(),
                    reasoning = r.Reasoning,
                    priority = r.Priority
                }).ToList();
            }

            // Generar análisis de gap de habilidades
            _logger.LogDebug("Generating skill gap analysis for session {SessionId}", job.SessionId);
            var skillGap = await analysisService.AnalyzeSkillGapAsync(job.SessionId);
            results["skillGapAnalysis"] = new
            {
                overallCoverage = skillGap.OverallCoverage,
                scarceSkillsCount = skillGap.ScarceSkills.Count,
                missingSkillsCount = skillGap.MissingSkills.Count,
                abundantSkillsCount = skillGap.AbundantSkills.Count
            };

            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // Actualizar estado de la sesión
            await sessionService.UpdateStatusAsync(job.SessionId, SessionStatus.Completed, 
                $"Analysis completed in {processingTime:F0}ms");

            // Notificar finalización del análisis
            using (var notificationScope2 = _serviceScopeFactory.CreateScope())
            {
                var notificationService = notificationScope2.ServiceProvider.GetRequiredService<IProcessingNotificationService>();
                await notificationService.NotifySessionProgressAsync(job.SessionId, 100, "Analysis completed");
                await notificationService.NotifyAnalysisCompleteAsync(job.SessionId, results);
                await notificationService.NotifySessionStatusChangedAsync(job.SessionId, "Completed", "Analysis completed successfully");
            }

            _logger.LogInformation("Analysis completed for session {SessionId} in {ProcessingTime}ms", 
                job.SessionId, processingTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process analysis for session {SessionId}", job.SessionId);

            // Actualizar estado de error
            await sessionService.UpdateStatusAsync(job.SessionId, SessionStatus.Failed, 
                $"Analysis failed: {ex.Message}");

            // Notificar error
            using (var notificationScope3 = _serviceScopeFactory.CreateScope())
            {
                var notificationService = notificationScope3.ServiceProvider.GetRequiredService<IProcessingNotificationService>();
                await notificationService.NotifyProcessingErrorAsync(job.SessionId, 
                    $"Analysis failed: {ex.Message}");
            }
            using (var notificationScope4 = _serviceScopeFactory.CreateScope())
            {
                var notificationService = notificationScope4.ServiceProvider.GetRequiredService<IProcessingNotificationService>();
                await notificationService.NotifySessionStatusChangedAsync(job.SessionId, "Failed", 
                    "Analysis failed");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Session Analysis Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}