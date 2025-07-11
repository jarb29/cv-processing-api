using CVProcessing.Core.Entities;

namespace CVProcessing.Core.Interfaces;

/// <summary>
/// Servicio para integración con OpenAI para análisis de CVs
/// </summary>
public interface IOpenAIService
{
    /// <summary>
    /// Extraer datos estructurados de un CV usando OpenAI
    /// </summary>
    /// <param name="documentText">Texto extraído del CV</param>
    /// <param name="jobOffer">Oferta laboral de referencia</param>
    /// <returns>Datos estructurados del CV</returns>
    Task<CVData> ExtractCVDataAsync(string documentText, JobOffer jobOffer);
    
    /// <summary>
    /// Generar una comparación detallada entre un CV y una oferta laboral
    /// </summary>
    /// <param name="cvData">Datos del CV</param>
    /// <param name="jobOffer">Oferta laboral</param>
    /// <returns>Análisis de coincidencia</returns>
    Task<CandidateComparison> GenerateComparisonAsync(CVData cvData, JobOffer jobOffer);
    
    /// <summary>
    /// Generar un resumen ejecutivo de un candidato
    /// </summary>
    /// <param name="cvData">Datos del CV</param>
    /// <param name="jobOffer">Oferta laboral</param>
    /// <returns>Resumen ejecutivo</returns>
    Task<string> GenerateExecutiveSummaryAsync(CVData cvData, JobOffer jobOffer);
    
    /// <summary>
    /// Verificar la salud de la conexión con OpenAI
    /// </summary>
    /// <returns>True si la conexión es exitosa</returns>
    Task<bool> HealthCheckAsync();
    
    /// <summary>
    /// Obtener información de uso de tokens
    /// </summary>
    /// <returns>Estadísticas de uso</returns>
    Task<OpenAIUsageStats> GetUsageStatsAsync();
}

/// <summary>
/// Estadísticas de uso de OpenAI
/// </summary>
public record OpenAIUsageStats
{
    /// <summary>
    /// Tokens utilizados en el período actual
    /// </summary>
    public int TokensUsed { get; init; }
    
    /// <summary>
    /// Límite de tokens disponible
    /// </summary>
    public int TokenLimit { get; init; }
    
    /// <summary>
    /// Número de requests realizados
    /// </summary>
    public int RequestCount { get; init; }
    
    /// <summary>
    /// Costo estimado en USD
    /// </summary>
    public decimal EstimatedCost { get; init; }
    
    /// <summary>
    /// Fecha de último reset del contador
    /// </summary>
    public DateTime LastReset { get; init; }
}