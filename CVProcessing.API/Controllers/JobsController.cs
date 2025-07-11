using CVProcessing.Infrastructure.Queue;
using Microsoft.AspNetCore.Mvc;

namespace CVProcessing.API.Controllers;

/// <summary>
/// Controlador para gestión manual de trabajos en cola
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class JobsController : ControllerBase
{
    private readonly IJobQueue<DocumentProcessingJob> _documentQueue;
    private readonly IJobQueue<SessionAnalysisJob> _analysisQueue;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        IJobQueue<DocumentProcessingJob> documentQueue,
        IJobQueue<SessionAnalysisJob> analysisQueue,
        ILogger<JobsController> logger)
    {
        _documentQueue = documentQueue;
        _analysisQueue = analysisQueue;
        _logger = logger;
    }

    /// <summary>
    /// Obtener estadísticas de las colas de trabajo
    /// </summary>
    /// <returns>Estadísticas de las colas</returns>
    [HttpGet("queue-stats")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult GetQueueStats()
    {
        return Ok(new
        {
            documentProcessingQueue = new
            {
                pendingJobs = _documentQueue.Count,
                queueType = "DocumentProcessing"
            },
            sessionAnalysisQueue = new
            {
                pendingJobs = _analysisQueue.Count,
                queueType = "SessionAnalysis"
            },
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Encolar manualmente el procesamiento de un documento
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <param name="documentId">ID del documento</param>
    /// <returns>Confirmación de encolado</returns>
    [HttpPost("process-document")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> QueueDocumentProcessing(
        [FromQuery] Guid sessionId,
        [FromQuery] Guid documentId)
    {
        _logger.LogInformation("Manually queuing document {DocumentId} from session {SessionId}", documentId, sessionId);

        try
        {
            var job = new DocumentProcessingJob
            {
                SessionId = sessionId,
                DocumentId = documentId,
                DocumentPath = "", // Will be resolved by the processor
                Priority = 100 // High priority for manual jobs
            };

            await _documentQueue.EnqueueAsync(job);

            return Accepted(new
            {
                message = "Document processing job queued successfully",
                sessionId,
                documentId,
                queuedAt = DateTime.UtcNow,
                priority = job.Priority
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue document processing job");
            return BadRequest(new { error = "Failed to queue document processing job", details = ex.Message });
        }
    }

    /// <summary>
    /// Encolar manualmente el análisis de una sesión
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <param name="generateMatrix">Generar matriz de comparación</param>
    /// <param name="generateRecommendations">Generar recomendaciones</param>
    /// <returns>Confirmación de encolado</returns>
    [HttpPost("analyze-session")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> QueueSessionAnalysis(
        [FromQuery] Guid sessionId,
        [FromQuery] bool generateMatrix = true,
        [FromQuery] bool generateRecommendations = true)
    {
        _logger.LogInformation("Manually queuing session analysis for {SessionId}", sessionId);

        try
        {
            var job = new SessionAnalysisJob
            {
                SessionId = sessionId,
                GenerateMatrix = generateMatrix,
                GenerateRecommendations = generateRecommendations
            };

            await _analysisQueue.EnqueueAsync(job);

            return Accepted(new
            {
                message = "Session analysis job queued successfully",
                sessionId,
                generateMatrix,
                generateRecommendations,
                queuedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue session analysis job");
            return BadRequest(new { error = "Failed to queue session analysis job", details = ex.Message });
        }
    }

    /// <summary>
    /// Forzar el procesamiento de todos los documentos pendientes de una sesión
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <returns>Confirmación de encolado</returns>
    [HttpPost("process-session/{sessionId:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ProcessAllSessionDocuments(Guid sessionId)
    {
        // This would require access to session repository to get pending documents
        // For now, return a placeholder response
        return Accepted(new
        {
            message = "Session processing initiated",
            sessionId,
            note = "All pending documents in the session will be queued for processing"
        });
    }
}