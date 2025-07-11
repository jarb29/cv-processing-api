using CVProcessing.Core.Entities;

namespace CVProcessing.Core.Interfaces;

/// <summary>
/// Servicio para análisis y comparación de candidatos
/// </summary>
public interface IAnalysisService
{
    /// <summary>
    /// Generar matriz de comparación para una sesión
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <returns>Matriz de comparación generada</returns>
    Task<ComparisonMatrix> GenerateComparisonMatrixAsync(Guid sessionId);
    
    /// <summary>
    /// Calcular puntuación de un CV contra una oferta laboral
    /// </summary>
    /// <param name="cvData">Datos del CV</param>
    /// <param name="jobOffer">Oferta laboral</param>
    /// <returns>Puntuación calculada</returns>
    Task<CVScore> CalculateScoreAsync(CVData cvData, JobOffer jobOffer);
    
    /// <summary>
    /// Comparar un candidato específico contra la oferta laboral
    /// </summary>
    /// <param name="cvData">Datos del CV</param>
    /// <param name="jobOffer">Oferta laboral</param>
    /// <returns>Comparación detallada</returns>
    Task<CandidateComparison> CompareCandidate(CVData cvData, JobOffer jobOffer);
    
    /// <summary>
    /// Generar ranking de candidatos
    /// </summary>
    /// <param name="candidates">Lista de candidatos</param>
    /// <param name="sortBy">Criterio de ordenamiento</param>
    /// <param name="ascending">Orden ascendente o descendente</param>
    /// <returns>Lista ordenada de candidatos</returns>
    Task<List<CandidateComparison>> RankCandidatesAsync(
        List<CandidateComparison> candidates, 
        RankingCriteria sortBy = RankingCriteria.Overall,
        bool ascending = false);
    
    /// <summary>
    /// Generar estadísticas de una sesión
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <returns>Estadísticas calculadas</returns>
    Task<ComparisonStatistics> GenerateStatisticsAsync(Guid sessionId);
    
    /// <summary>
    /// Identificar habilidades más demandadas vs disponibles
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <returns>Análisis de gap de habilidades</returns>
    Task<SkillGapAnalysis> AnalyzeSkillGapAsync(Guid sessionId);
    
    /// <summary>
    /// Generar recomendaciones de contratación
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <param name="topN">Número de candidatos top a recomendar</param>
    /// <returns>Lista de recomendaciones</returns>
    Task<List<HiringRecommendationDetail>> GenerateHiringRecommendationsAsync(Guid sessionId, int topN = 5);
}

/// <summary>
/// Criterios de ranking de candidatos
/// </summary>
public enum RankingCriteria
{
    Overall,
    Experience,
    Skills,
    Education,
    JobMatch
}

/// <summary>
/// Análisis de gap de habilidades
/// </summary>
public record SkillGapAnalysis
{
    /// <summary>
    /// Habilidades requeridas que están escasas
    /// </summary>
    public List<SkillGap> ScarceSkills { get; init; } = [];
    
    /// <summary>
    /// Habilidades abundantes en los candidatos
    /// </summary>
    public List<SkillFrequency> AbundantSkills { get; init; } = [];
    
    /// <summary>
    /// Habilidades completamente ausentes
    /// </summary>
    public List<string> MissingSkills { get; init; } = [];
    
    /// <summary>
    /// Porcentaje general de cobertura de habilidades
    /// </summary>
    public double OverallCoverage { get; init; }
}

/// <summary>
/// Gap de una habilidad específica
/// </summary>
public record SkillGap
{
    public required string Skill { get; init; }
    public int Required { get; init; }
    public int Available { get; init; }
    public double GapPercentage { get; init; }
}

/// <summary>
/// Recomendación detallada de contratación
/// </summary>
public record HiringRecommendationDetail
{
    public required CandidateComparison Candidate { get; init; }
    public required HiringRecommendation Recommendation { get; init; }
    public required string Reasoning { get; init; }
    public List<string> NextSteps { get; init; } = [];
    public int Priority { get; init; }
}