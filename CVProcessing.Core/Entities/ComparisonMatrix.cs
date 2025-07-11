namespace CVProcessing.Core.Entities;

/// <summary>
/// Matriz de comparación entre candidatos de una sesión
/// </summary>
public record ComparisonMatrix
{
    /// <summary>
    /// ID de la sesión a la que pertenece esta matriz
    /// </summary>
    public required Guid SessionId { get; init; }
    
    /// <summary>
    /// Lista de candidatos ordenados por puntuación
    /// </summary>
    public List<CandidateComparison> Candidates { get; init; } = [];
    
    /// <summary>
    /// Estadísticas generales de la comparación
    /// </summary>
    public required ComparisonStatistics Statistics { get; init; }
    
    /// <summary>
    /// Fecha de generación de la matriz
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Tiempo que tomó generar la matriz en milisegundos
    /// </summary>
    public long GenerationTimeMs { get; init; }
}

/// <summary>
/// Comparación individual de un candidato
/// </summary>
public record CandidateComparison
{
    /// <summary>
    /// ID del documento (CV)
    /// </summary>
    public required Guid DocumentId { get; init; }
    
    /// <summary>
    /// Nombre del candidato
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Email del candidato (si está disponible)
    /// </summary>
    public string? Email { get; init; }
    
    /// <summary>
    /// Puntuación general del candidato
    /// </summary>
    public required int OverallScore { get; init; }
    
    /// <summary>
    /// Puntuaciones detalladas por categoría
    /// </summary>
    public required CVScore Scores { get; init; }
    
    /// <summary>
    /// Posición en el ranking (1 = mejor)
    /// </summary>
    public int Ranking { get; init; }
    
    /// <summary>
    /// Habilidades que coinciden con la oferta laboral
    /// </summary>
    public List<string> MatchingSkills { get; init; } = [];
    
    /// <summary>
    /// Habilidades que faltan según la oferta laboral
    /// </summary>
    public List<string> MissingSkills { get; init; } = [];
    
    /// <summary>
    /// Años de experiencia relevante
    /// </summary>
    public int RelevantExperienceYears { get; init; }
    
    /// <summary>
    /// Resumen de fortalezas del candidato
    /// </summary>
    public List<string> Strengths { get; init; } = [];
    
    /// <summary>
    /// Áreas de mejora o debilidades
    /// </summary>
    public List<string> Weaknesses { get; init; } = [];
    
    /// <summary>
    /// Recomendación de contratación
    /// </summary>
    public HiringRecommendation Recommendation { get; init; }
}

/// <summary>
/// Estadísticas de la comparación
/// </summary>
public record ComparisonStatistics
{
    /// <summary>
    /// Total de candidatos analizados
    /// </summary>
    public int TotalCandidates { get; init; }
    
    /// <summary>
    /// Puntuación promedio
    /// </summary>
    public double AverageScore { get; init; }
    
    /// <summary>
    /// Puntuación más alta
    /// </summary>
    public int HighestScore { get; init; }
    
    /// <summary>
    /// Puntuación más baja
    /// </summary>
    public int LowestScore { get; init; }
    
    /// <summary>
    /// Desviación estándar de las puntuaciones
    /// </summary>
    public double ScoreStandardDeviation { get; init; }
    
    /// <summary>
    /// Habilidades más comunes entre candidatos
    /// </summary>
    public List<SkillFrequency> CommonSkills { get; init; } = [];
    
    /// <summary>
    /// Distribución de años de experiencia
    /// </summary>
    public ExperienceDistribution ExperienceDistribution { get; init; } = new();
}

/// <summary>
/// Frecuencia de una habilidad entre candidatos
/// </summary>
public record SkillFrequency
{
    public required string Skill { get; init; }
    public int Count { get; init; }
    public double Percentage { get; init; }
}

/// <summary>
/// Distribución de experiencia entre candidatos
/// </summary>
public record ExperienceDistribution
{
    public int Junior { get; init; } // 0-2 años
    public int Mid { get; init; }    // 3-5 años
    public int Senior { get; init; } // 6+ años
}

/// <summary>
/// Recomendación de contratación
/// </summary>
public enum HiringRecommendation
{
    /// <summary>
    /// Candidato altamente recomendado
    /// </summary>
    HighlyRecommended,
    
    /// <summary>
    /// Candidato recomendado
    /// </summary>
    Recommended,
    
    /// <summary>
    /// Candidato considerado con reservas
    /// </summary>
    Consider,
    
    /// <summary>
    /// Candidato no recomendado
    /// </summary>
    NotRecommended
}