using CVProcessing.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CVProcessing.API.Controllers;

/// <summary>
/// Controlador para análisis y comparación de candidatos
/// </summary>
[ApiController]
[Route("api/sessions/{sessionId:guid}/[controller]")]
[Produces("application/json")]
public class AnalysisController : ControllerBase
{
    private readonly IAnalysisService _analysisService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<AnalysisController> _logger;

    public AnalysisController(
        IAnalysisService analysisService,
        ISessionService sessionService,
        ILogger<AnalysisController> logger)
    {
        _analysisService = analysisService;
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Generar matriz de comparación de candidatos
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <returns>Matriz de comparación completa</returns>
    [HttpPost("matrix")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GenerateComparisonMatrix(Guid sessionId)
    {
        _logger.LogInformation("Generating comparison matrix for session: {SessionId}", sessionId);

        var sessionExists = await _sessionService.ExistsAsync(sessionId);
        if (!sessionExists)
            return NotFound($"Session {sessionId} not found");

        try
        {
            var matrix = await _analysisService.GenerateComparisonMatrixAsync(sessionId);
            return Ok(new
            {
                sessionId = matrix.SessionId,
                candidates = matrix.Candidates.Select(c => new
                {
                    documentId = c.DocumentId,
                    name = c.Name,
                    email = c.Email,
                    overallScore = c.OverallScore,
                    scores = new
                    {
                        experience = c.Scores.Experience,
                        skills = c.Scores.Skills,
                        education = c.Scores.Education,
                        jobMatch = c.Scores.JobMatch
                    },
                    ranking = c.Ranking,
                    matchingSkills = c.MatchingSkills,
                    missingSkills = c.MissingSkills,
                    relevantExperienceYears = c.RelevantExperienceYears,
                    strengths = c.Strengths,
                    weaknesses = c.Weaknesses,
                    recommendation = c.Recommendation.ToString()
                }),
                statistics = new
                {
                    totalCandidates = matrix.Statistics.TotalCandidates,
                    averageScore = matrix.Statistics.AverageScore,
                    highestScore = matrix.Statistics.HighestScore,
                    lowestScore = matrix.Statistics.LowestScore,
                    scoreStandardDeviation = matrix.Statistics.ScoreStandardDeviation,
                    commonSkills = matrix.Statistics.CommonSkills,
                    experienceDistribution = matrix.Statistics.ExperienceDistribution
                },
                generatedAt = matrix.GeneratedAt,
                generationTimeMs = matrix.GenerationTimeMs
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to generate comparison matrix for session {SessionId}", sessionId);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Obtener matriz de comparación existente
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <returns>Matriz de comparación</returns>
    [HttpGet("matrix")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetComparisonMatrix(Guid sessionId)
    {
        _logger.LogDebug("Getting comparison matrix for session: {SessionId}", sessionId);

        var session = await _sessionService.GetByIdAsync(sessionId);
        if (session == null)
            return NotFound($"Session {sessionId} not found");

        if (session.ComparisonMatrix == null)
            return NotFound("Comparison matrix not generated yet");

        var matrix = session.ComparisonMatrix;
        return Ok(new
        {
            sessionId = matrix.SessionId,
            candidates = matrix.Candidates.Select(c => new
            {
                documentId = c.DocumentId,
                name = c.Name,
                email = c.Email,
                overallScore = c.OverallScore,
                scores = new
                {
                    experience = c.Scores.Experience,
                    skills = c.Scores.Skills,
                    education = c.Scores.Education,
                    jobMatch = c.Scores.JobMatch
                },
                ranking = c.Ranking,
                matchingSkills = c.MatchingSkills,
                missingSkills = c.MissingSkills,
                relevantExperienceYears = c.RelevantExperienceYears,
                strengths = c.Strengths,
                weaknesses = c.Weaknesses,
                recommendation = c.Recommendation.ToString()
            }),
            statistics = new
            {
                totalCandidates = matrix.Statistics.TotalCandidates,
                averageScore = matrix.Statistics.AverageScore,
                highestScore = matrix.Statistics.HighestScore,
                lowestScore = matrix.Statistics.LowestScore,
                scoreStandardDeviation = matrix.Statistics.ScoreStandardDeviation,
                commonSkills = matrix.Statistics.CommonSkills,
                experienceDistribution = matrix.Statistics.ExperienceDistribution
            },
            generatedAt = matrix.GeneratedAt,
            generationTimeMs = matrix.GenerationTimeMs
        });
    }

    /// <summary>
    /// Obtener ranking de candidatos
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <param name="sortBy">Criterio de ordenamiento</param>
    /// <param name="order">Orden (asc/desc)</param>
    /// <param name="limit">Límite de resultados</param>
    /// <returns>Lista ordenada de candidatos</returns>
    [HttpGet("rankings")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetRankings(
        Guid sessionId,
        [FromQuery] string sortBy = "overall",
        [FromQuery] string order = "desc",
        [FromQuery] int limit = 50)
    {
        _logger.LogDebug("Getting rankings for session: {SessionId}", sessionId);

        var session = await _sessionService.GetByIdAsync(sessionId);
        if (session?.ComparisonMatrix == null)
            return NotFound("Comparison matrix not found");

        var criteria = sortBy.ToLowerInvariant() switch
        {
            "experience" => RankingCriteria.Experience,
            "skills" => RankingCriteria.Skills,
            "education" => RankingCriteria.Education,
            "jobmatch" => RankingCriteria.JobMatch,
            _ => RankingCriteria.Overall
        };

        var ascending = order.ToLowerInvariant() == "asc";
        var candidates = await _analysisService.RankCandidatesAsync(
            session.ComparisonMatrix.Candidates, 
            criteria, 
            ascending);

        var limitedCandidates = candidates.Take(limit).ToList();

        return Ok(new
        {
            sessionId,
            sortBy = criteria.ToString(),
            order = ascending ? "asc" : "desc",
            totalCandidates = candidates.Count,
            returnedCandidates = limitedCandidates.Count,
            candidates = limitedCandidates.Select(c => new
            {
                documentId = c.DocumentId,
                name = c.Name,
                email = c.Email,
                overallScore = c.OverallScore,
                ranking = c.Ranking,
                recommendation = c.Recommendation.ToString(),
                matchingSkills = c.MatchingSkills,
                missingSkills = c.MissingSkills,
                relevantExperienceYears = c.RelevantExperienceYears
            })
        });
    }

    /// <summary>
    /// Analizar gap de habilidades
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <returns>Análisis de gap de habilidades</returns>
    [HttpGet("skill-gap")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetSkillGapAnalysis(Guid sessionId)
    {
        _logger.LogDebug("Getting skill gap analysis for session: {SessionId}", sessionId);

        var sessionExists = await _sessionService.ExistsAsync(sessionId);
        if (!sessionExists)
            return NotFound($"Session {sessionId} not found");

        try
        {
            var analysis = await _analysisService.AnalyzeSkillGapAsync(sessionId);
            return Ok(new
            {
                sessionId,
                overallCoverage = analysis.OverallCoverage,
                scarceSkills = analysis.ScarceSkills.Select(s => new
                {
                    skill = s.Skill,
                    required = s.Required,
                    available = s.Available,
                    gapPercentage = s.GapPercentage
                }),
                abundantSkills = analysis.AbundantSkills.Select(s => new
                {
                    skill = s.Skill,
                    count = s.Count,
                    percentage = s.Percentage
                }),
                missingSkills = analysis.MissingSkills
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to analyze skill gap for session {SessionId}", sessionId);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Generar recomendaciones de contratación
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <param name="topN">Número de candidatos top</param>
    /// <returns>Recomendaciones de contratación</returns>
    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetHiringRecommendations(
        Guid sessionId,
        [FromQuery] int topN = 5)
    {
        _logger.LogDebug("Getting hiring recommendations for session: {SessionId}", sessionId);

        var sessionExists = await _sessionService.ExistsAsync(sessionId);
        if (!sessionExists)
            return NotFound($"Session {sessionId} not found");

        try
        {
            var recommendations = await _analysisService.GenerateHiringRecommendationsAsync(sessionId, topN);
            return Ok(new
            {
                sessionId,
                totalRecommendations = recommendations.Count,
                recommendations = recommendations.Select(r => new
                {
                    candidate = new
                    {
                        documentId = r.Candidate.DocumentId,
                        name = r.Candidate.Name,
                        email = r.Candidate.Email,
                        overallScore = r.Candidate.OverallScore,
                        ranking = r.Candidate.Ranking
                    },
                    recommendation = r.Recommendation.ToString(),
                    reasoning = r.Reasoning,
                    nextSteps = r.NextSteps,
                    priority = r.Priority
                })
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to generate recommendations for session {SessionId}", sessionId);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Obtener estadísticas de análisis
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <returns>Estadísticas del análisis</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetStatistics(Guid sessionId)
    {
        _logger.LogDebug("Getting statistics for session: {SessionId}", sessionId);

        try
        {
            var statistics = await _analysisService.GenerateStatisticsAsync(sessionId);
            return Ok(new
            {
                sessionId,
                totalCandidates = statistics.TotalCandidates,
                averageScore = statistics.AverageScore,
                highestScore = statistics.HighestScore,
                lowestScore = statistics.LowestScore,
                scoreStandardDeviation = statistics.ScoreStandardDeviation,
                commonSkills = statistics.CommonSkills.Select(s => new
                {
                    skill = s.Skill,
                    count = s.Count,
                    percentage = s.Percentage
                }),
                experienceDistribution = new
                {
                    junior = statistics.ExperienceDistribution.Junior,
                    mid = statistics.ExperienceDistribution.Mid,
                    senior = statistics.ExperienceDistribution.Senior
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to get statistics for session {SessionId}", sessionId);
            return NotFound(ex.Message);
        }
    }
}