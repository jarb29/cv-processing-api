namespace CVProcessing.Infrastructure.Queue;

/// <summary>
/// Interface para sistema de colas de trabajos
/// </summary>
public interface IJobQueue<T>
{
    /// <summary>
    /// Encolar un trabajo
    /// </summary>
    Task EnqueueAsync(T job);
    
    /// <summary>
    /// Desencolar un trabajo
    /// </summary>
    Task<T?> DequeueAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Obtener número de trabajos pendientes
    /// </summary>
    int Count { get; }
}

/// <summary>
/// Trabajo de procesamiento de documento
/// </summary>
public record DocumentProcessingJob
{
    public required Guid SessionId { get; init; }
    public required Guid DocumentId { get; init; }
    public required string DocumentPath { get; init; }
    public DateTime QueuedAt { get; init; } = DateTime.UtcNow;
    public int Priority { get; init; } = 0;
}

/// <summary>
/// Trabajo de análisis de sesión
/// </summary>
public record SessionAnalysisJob
{
    public required Guid SessionId { get; init; }
    public DateTime QueuedAt { get; init; } = DateTime.UtcNow;
    public bool GenerateMatrix { get; init; } = true;
    public bool GenerateRecommendations { get; init; } = true;
}