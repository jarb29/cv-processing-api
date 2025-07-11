using CVProcessing.Core.Enums;

namespace CVProcessing.Core.Entities;

/// <summary>
/// Representa una sesión de procesamiento de CVs
/// </summary>
public class Session
{
    /// <summary>
    /// Identificador único de la sesión
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Oferta laboral de referencia para esta sesión
    /// </summary>
    public required JobOffer JobOffer { get; init; }
    
    /// <summary>
    /// Lista de documentos (CVs) en esta sesión
    /// </summary>
    public List<Document> Documents { get; init; } = [];
    
    /// <summary>
    /// Estado actual de la sesión
    /// </summary>
    public SessionStatus Status { get; set; } = SessionStatus.Created;
    
    /// <summary>
    /// Matriz de comparación (null hasta que se complete el análisis)
    /// </summary>
    public ComparisonMatrix? ComparisonMatrix { get; set; }
    
    /// <summary>
    /// Progreso del procesamiento (0-100)
    /// </summary>
    public int Progress { get; set; } = 0;
    
    /// <summary>
    /// Mensaje de estado actual
    /// </summary>
    public string? StatusMessage { get; set; }
    
    /// <summary>
    /// Fecha de creación de la sesión
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Fecha de última actualización
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Fecha de finalización del procesamiento
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Tiempo total de procesamiento en milisegundos
    /// </summary>
    public long? TotalProcessingTimeMs { get; set; }
    
    /// <summary>
    /// Estadísticas de la sesión
    /// </summary>
    public SessionStatistics Statistics { get; set; } = new();
}

/// <summary>
/// Estadísticas de una sesión de procesamiento
/// </summary>
public record SessionStatistics
{
    /// <summary>
    /// Total de documentos cargados
    /// </summary>
    public int TotalDocuments { get; init; }
    
    /// <summary>
    /// Documentos procesados exitosamente
    /// </summary>
    public int ProcessedDocuments { get; init; }
    
    /// <summary>
    /// Documentos que fallaron en el procesamiento
    /// </summary>
    public int FailedDocuments { get; init; }
    
    /// <summary>
    /// Puntuación promedio de todos los CVs
    /// </summary>
    public double AverageScore { get; init; }
    
    /// <summary>
    /// Puntuación más alta obtenida
    /// </summary>
    public int HighestScore { get; init; }
    
    /// <summary>
    /// Puntuación más baja obtenida
    /// </summary>
    public int LowestScore { get; init; }
}