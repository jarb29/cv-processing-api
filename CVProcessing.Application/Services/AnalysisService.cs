using CVProcessing.Core.Entities;
using CVProcessing.Core.Interfaces;
using CVProcessing.Infrastructure.Storage;
using Microsoft.Extensions.Logging;

namespace CVProcessing.Application.Services;

/// <summary>
/// Servicio de aplicación para análisis y comparación de candidatos
/// </summary>
public class AnalysisService : IAnalysisService
{
    private readonly SessionRepository _sessionRepository;
    private readonly IOpenAIService _openAIService;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<AnalysisService> _logger;

    public AnalysisService(
        SessionRepository sessionRepository,
        IOpenAIService openAIService,
        IFileStorage fileStorage,
        ILogger<AnalysisService> logger)
    {
        _sessionRepository = sessionRepository;
        _openAIService = openAIService;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<ComparisonMatrix> GenerateComparisonMatrixAsync(Guid sessionId)
    {
        _logger.LogInformation("Generating comparison matrix for session: {SessionId}", sessionId);

        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new InvalidOperationException($"Session {sessionId} not found");

        var processedDocuments = session.Documents
            .Where(d => d.Status == Core.Enums.DocumentStatus.Processed && d.ExtractedData != null)
            .ToList();

        if (!processedDocuments.Any())
            throw new InvalidOperationException("No processed documents found in session");

        var candidates = new List<CandidateComparison>();

        // Generar comparación para cada candidato
        foreach (var document in processedDocuments)
        {
            var comparison = await GenerateComparisonForDocument(document, session.JobOffer);
            candidates.Add(comparison);
        }

        // Ordenar por puntuación y asignar ranking
        candidates = candidates.OrderByDescending(c => c.OverallScore).ToList();
        for (int i = 0; i < candidates.Count; i++)
        {
            candidates[i] = candidates[i] with { Ranking = i + 1 };
        }

        // Generar estadísticas
        var statistics = GenerateStatistics(candidates);

        var matrix = new ComparisonMatrix
        {
            SessionId = sessionId,
            Candidates = candidates,
            Statistics = statistics,
            GeneratedAt = DateTime.UtcNow
        };

        // Guardar matriz en archivo
        await SaveComparisonMatrix(sessionId, matrix);

        // Actualizar sesión
        session.ComparisonMatrix = matrix;
        await _sessionRepository.UpdateAsync(session);

        _logger.LogInformation("Comparison matrix generated successfully for session: {SessionId}", sessionId);
        return matrix;
    }

    public async Task<CVScore> CalculateScoreAsync(CVData cvData, JobOffer jobOffer)
    {
        _logger.LogDebug("Calculating score for candidate: {Name}", cvData.PersonalInfo.Name);

        // Puntuación por experiencia
        var experienceScore = CalculateExperienceScore(cvData.Experience, jobOffer.MinExperienceYears);
        
        // Puntuación por habilidades
        var skillsScore = CalculateSkillsScore(cvData.Skills, jobOffer.RequiredSkills, jobOffer.PreferredSkills);
        
        // Puntuación por educación
        var educationScore = CalculateEducationScore(cvData.Education, jobOffer.EducationLevel);
        
        // Puntuación de coincidencia con el trabajo
        var jobMatchScore = CalculateJobMatchScore(cvData, jobOffer);
        
        // Puntuación general (promedio ponderado)
        var overallScore = (int)Math.Round(
            (experienceScore * 0.3) + 
            (skillsScore * 0.4) + 
            (educationScore * 0.2) + 
            (jobMatchScore * 0.1)
        );

        return new CVScore
        {
            Overall = overallScore,
            Experience = experienceScore,
            Skills = skillsScore,
            Education = educationScore,
            JobMatch = jobMatchScore
        };
    }

    public async Task<CandidateComparison> CompareCandidate(CVData cvData, JobOffer jobOffer)
    {
        _logger.LogDebug("Comparing candidate: {Name}", cvData.PersonalInfo.Name);

        var score = await CalculateScoreAsync(cvData, jobOffer);
        
        // Identificar habilidades coincidentes y faltantes
        var matchingSkills = cvData.Skills.Intersect(jobOffer.RequiredSkills, StringComparer.OrdinalIgnoreCase).ToList();
        var missingSkills = jobOffer.RequiredSkills.Except(cvData.Skills, StringComparer.OrdinalIgnoreCase).ToList();
        
        // Calcular años de experiencia relevante
        var relevantYears = CalculateRelevantExperience(cvData.Experience, jobOffer);
        
        // Generar fortalezas y debilidades
        var strengths = GenerateStrengths(cvData, jobOffer);
        var weaknesses = GenerateWeaknesses(cvData, jobOffer);
        
        // Determinar recomendación
        var recommendation = DetermineRecommendation(score.Overall, matchingSkills.Count, jobOffer.RequiredSkills.Count);

        return new CandidateComparison
        {
            DocumentId = Guid.NewGuid(), // Esto debería ser establecido por el llamador
            Name = cvData.PersonalInfo.Name,
            Email = cvData.PersonalInfo.Email,
            OverallScore = score.Overall,
            Scores = score,
            MatchingSkills = matchingSkills,
            MissingSkills = missingSkills,
            RelevantExperienceYears = relevantYears,
            Strengths = strengths,
            Weaknesses = weaknesses,
            Recommendation = recommendation
        };
    }

    public async Task<List<CandidateComparison>> RankCandidatesAsync(
        List<CandidateComparison> candidates, 
        RankingCriteria sortBy = RankingCriteria.Overall,
        bool ascending = false)
    {
        _logger.LogDebug("Ranking {Count} candidates by {Criteria}", candidates.Count, sortBy);

        var sorted = sortBy switch
        {
            RankingCriteria.Overall => candidates.OrderBy(c => c.OverallScore),
            RankingCriteria.Experience => candidates.OrderBy(c => c.Scores.Experience),
            RankingCriteria.Skills => candidates.OrderBy(c => c.Scores.Skills),
            RankingCriteria.Education => candidates.OrderBy(c => c.Scores.Education),
            RankingCriteria.JobMatch => candidates.OrderBy(c => c.Scores.JobMatch),
            _ => candidates.OrderBy(c => c.OverallScore)
        };

        var result = ascending ? sorted.ToList() : sorted.Reverse().ToList();
        
        // Actualizar rankings
        for (int i = 0; i < result.Count; i++)
        {
            result[i] = result[i] with { Ranking = i + 1 };
        }

        return result;
    }

    public async Task<ComparisonStatistics> GenerateStatisticsAsync(Guid sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session?.ComparisonMatrix == null)
            throw new InvalidOperationException("No comparison matrix found for session");

        return session.ComparisonMatrix.Statistics;
    }

    public async Task<SkillGapAnalysis> AnalyzeSkillGapAsync(Guid sessionId)
    {
        _logger.LogInformation("Analyzing skill gap for session: {SessionId}", sessionId);

        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new InvalidOperationException($"Session {sessionId} not found");

        var processedDocuments = session.Documents
            .Where(d => d.ExtractedData != null)
            .ToList();

        var allCandidateSkills = processedDocuments
            .SelectMany(d => d.ExtractedData!.Skills)
            .ToList();

        var requiredSkills = session.JobOffer.RequiredSkills;
        var skillFrequency = allCandidateSkills
            .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count());

        var scarceSkills = requiredSkills
            .Select(skill => new SkillGap
            {
                Skill = skill,
                Required = 1,
                Available = skillFrequency.GetValueOrDefault(skill, 0),
                GapPercentage = 1.0 - (skillFrequency.GetValueOrDefault(skill, 0) / (double)processedDocuments.Count)
            })
            .Where(sg => sg.Available < processedDocuments.Count * 0.5) // Menos del 50% tiene la habilidad
            .ToList();

        var abundantSkills = skillFrequency
            .Where(kv => kv.Value >= processedDocuments.Count * 0.7) // Más del 70% tiene la habilidad
            .Select(kv => new SkillFrequency
            {
                Skill = kv.Key,
                Count = kv.Value,
                Percentage = (kv.Value / (double)processedDocuments.Count) * 100
            })
            .ToList();

        var missingSkills = requiredSkills
            .Where(skill => !skillFrequency.ContainsKey(skill))
            .ToList();

        var coverage = requiredSkills.Count > 0 
            ? (requiredSkills.Count(skill => skillFrequency.ContainsKey(skill)) / (double)requiredSkills.Count) * 100
            : 100;

        return new SkillGapAnalysis
        {
            ScarceSkills = scarceSkills,
            AbundantSkills = abundantSkills,
            MissingSkills = missingSkills,
            OverallCoverage = coverage
        };
    }

    public async Task<List<HiringRecommendationDetail>> GenerateHiringRecommendationsAsync(Guid sessionId, int topN = 5)
    {
        _logger.LogInformation("Generating hiring recommendations for session: {SessionId}", sessionId);

        var matrix = await GenerateComparisonMatrixAsync(sessionId);
        var topCandidates = matrix.Candidates.Take(topN).ToList();

        var recommendations = new List<HiringRecommendationDetail>();

        foreach (var candidate in topCandidates)
        {
            var reasoning = GenerateRecommendationReasoning(candidate);
            var nextSteps = GenerateNextSteps(candidate);

            recommendations.Add(new HiringRecommendationDetail
            {
                Candidate = candidate,
                Recommendation = candidate.Recommendation,
                Reasoning = reasoning,
                NextSteps = nextSteps,
                Priority = candidate.Ranking
            });
        }

        return recommendations;
    }

    #region Private Helper Methods

    private async Task<CandidateComparison> GenerateComparisonForDocument(Document document, JobOffer jobOffer)
    {
        var cvData = document.ExtractedData!;
        var comparison = await CompareCandidate(cvData, jobOffer);
        return comparison with { DocumentId = document.Id };
    }

    private static ComparisonStatistics GenerateStatistics(List<CandidateComparison> candidates)
    {
        if (!candidates.Any()) 
            return new ComparisonStatistics();

        var scores = candidates.Select(c => c.OverallScore).ToList();
        var average = scores.Average();
        var variance = scores.Select(s => Math.Pow(s - average, 2)).Average();
        var stdDev = Math.Sqrt(variance);

        var skillCounts = candidates
            .SelectMany(c => c.MatchingSkills)
            .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SkillFrequency
            {
                Skill = g.Key,
                Count = g.Count(),
                Percentage = (g.Count() / (double)candidates.Count) * 100
            })
            .OrderByDescending(sf => sf.Count)
            .Take(10)
            .ToList();

        var experienceDistribution = new ExperienceDistribution
        {
            Junior = candidates.Count(c => c.RelevantExperienceYears <= 2),
            Mid = candidates.Count(c => c.RelevantExperienceYears is > 2 and <= 5),
            Senior = candidates.Count(c => c.RelevantExperienceYears > 5)
        };

        return new ComparisonStatistics
        {
            TotalCandidates = candidates.Count,
            AverageScore = average,
            HighestScore = scores.Max(),
            LowestScore = scores.Min(),
            ScoreStandardDeviation = stdDev,
            CommonSkills = skillCounts,
            ExperienceDistribution = experienceDistribution
        };
    }

    private async Task SaveComparisonMatrix(Guid sessionId, ComparisonMatrix matrix)
    {
        var analysisPath = Path.Combine(Core.Constants.StoragePaths.Sessions, sessionId.ToString(), Core.Constants.StoragePaths.Analysis);
        await _fileStorage.CreateDirectoryAsync(analysisPath);
        
        var matrixPath = Path.Combine(analysisPath, Core.Constants.StoragePaths.ComparisonMatrix);
        var jsonData = System.Text.Json.JsonSerializer.Serialize(matrix, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await _fileStorage.SaveTextAsync(matrixPath, jsonData);
    }

    private static int CalculateExperienceScore(List<Experience> experiences, int minRequired)
    {
        var totalYears = experiences.Sum(e => CalculateExperienceYears(e));
        if (totalYears >= minRequired * 1.5) return 100;
        if (totalYears >= minRequired) return 80;
        if (totalYears >= minRequired * 0.7) return 60;
        return Math.Max(0, (int)(totalYears / (double)minRequired * 40));
    }

    private static int CalculateSkillsScore(List<string> candidateSkills, List<string> required, List<string> preferred)
    {
        var requiredMatches = candidateSkills.Intersect(required, StringComparer.OrdinalIgnoreCase).Count();
        var preferredMatches = candidateSkills.Intersect(preferred, StringComparer.OrdinalIgnoreCase).Count();
        
        var requiredScore = required.Count > 0 ? (requiredMatches / (double)required.Count) * 80 : 80;
        var preferredScore = preferred.Count > 0 ? (preferredMatches / (double)preferred.Count) * 20 : 20;
        
        return Math.Min(100, (int)(requiredScore + preferredScore));
    }

    private static int CalculateEducationScore(List<Education> education, string? requiredLevel)
    {
        if (string.IsNullOrEmpty(requiredLevel)) return 70; // Neutral si no se especifica
        
        var hasRequiredLevel = education.Any(e => 
            e.Degree.Contains(requiredLevel, StringComparison.OrdinalIgnoreCase));
        
        return hasRequiredLevel ? 100 : 50;
    }

    private static int CalculateJobMatchScore(CVData cvData, JobOffer jobOffer)
    {
        // Puntuación basada en coincidencia general con la descripción del trabajo
        var score = 70; // Base score
        
        // Bonus por habilidades coincidentes
        var skillMatches = cvData.Skills.Intersect(jobOffer.RequiredSkills, StringComparer.OrdinalIgnoreCase).Count();
        score += Math.Min(30, skillMatches * 5);
        
        return Math.Min(100, score);
    }

    private static int CalculateRelevantExperience(List<Experience> experiences, JobOffer jobOffer)
    {
        return experiences
            .Where(e => IsRelevantExperience(e, jobOffer))
            .Sum(CalculateExperienceYears);
    }

    private static bool IsRelevantExperience(Experience experience, JobOffer jobOffer)
    {
        var relevantTechnologies = experience.Technologies
            .Intersect(jobOffer.RequiredSkills, StringComparer.OrdinalIgnoreCase)
            .Any();
        
        var relevantPosition = jobOffer.RequiredSkills
            .Any(skill => experience.Position.Contains(skill, StringComparison.OrdinalIgnoreCase));
        
        return relevantTechnologies || relevantPosition;
    }

    private static int CalculateExperienceYears(Experience experience)
    {
        if (experience.StartDate.HasValue && experience.EndDate.HasValue)
        {
            return (int)(experience.EndDate.Value - experience.StartDate.Value).TotalDays / 365;
        }
        
        // Fallback: parse duration string
        if (!string.IsNullOrEmpty(experience.Duration))
        {
            var duration = experience.Duration.ToLowerInvariant();
            if (duration.Contains("año")) return int.TryParse(duration.Split(' ')[0], out var years) ? years : 1;
            if (duration.Contains("mes")) return 1; // Menos de un año
        }
        
        return 1; // Default
    }

    private static List<string> GenerateStrengths(CVData cvData, JobOffer jobOffer)
    {
        var strengths = new List<string>();
        
        var matchingSkills = cvData.Skills.Intersect(jobOffer.RequiredSkills, StringComparer.OrdinalIgnoreCase).ToList();
        if (matchingSkills.Count > jobOffer.RequiredSkills.Count * 0.7)
            strengths.Add($"Excelente coincidencia técnica ({matchingSkills.Count}/{jobOffer.RequiredSkills.Count} habilidades requeridas)");
        
        if (cvData.Experience.Count >= 3)
            strengths.Add("Amplia experiencia profesional");
        
        if (cvData.Education.Any())
            strengths.Add("Sólida formación académica");
        
        return strengths;
    }

    private static List<string> GenerateWeaknesses(CVData cvData, JobOffer jobOffer)
    {
        var weaknesses = new List<string>();
        
        var missingSkills = jobOffer.RequiredSkills.Except(cvData.Skills, StringComparer.OrdinalIgnoreCase).ToList();
        if (missingSkills.Any())
            weaknesses.Add($"Faltan habilidades clave: {string.Join(", ", missingSkills.Take(3))}");
        
        var totalExperience = cvData.Experience.Sum(CalculateExperienceYears);
        if (totalExperience < jobOffer.MinExperienceYears)
            weaknesses.Add($"Experiencia insuficiente ({totalExperience} vs {jobOffer.MinExperienceYears} años requeridos)");
        
        return weaknesses;
    }

    private static HiringRecommendation DetermineRecommendation(int overallScore, int matchingSkills, int totalRequired)
    {
        var skillMatch = totalRequired > 0 ? (double)matchingSkills / totalRequired : 1.0;
        
        if (overallScore >= 85 && skillMatch >= 0.8) return HiringRecommendation.HighlyRecommended;
        if (overallScore >= 70 && skillMatch >= 0.6) return HiringRecommendation.Recommended;
        if (overallScore >= 50) return HiringRecommendation.Consider;
        return HiringRecommendation.NotRecommended;
    }

    private static string GenerateRecommendationReasoning(CandidateComparison candidate)
    {
        return candidate.Recommendation switch
        {
            HiringRecommendation.HighlyRecommended => 
                $"Candidato excepcional con {candidate.OverallScore}% de coincidencia. Cumple con la mayoría de requisitos técnicos y tiene experiencia relevante.",
            HiringRecommendation.Recommended => 
                $"Buen candidato con {candidate.OverallScore}% de coincidencia. Perfil sólido que se ajusta bien al puesto.",
            HiringRecommendation.Consider => 
                $"Candidato a considerar con {candidate.OverallScore}% de coincidencia. Tiene potencial pero requiere evaluación adicional.",
            _ => 
                $"Candidato no recomendado con {candidate.OverallScore}% de coincidencia. No cumple con los requisitos mínimos."
        };
    }

    private static List<string> GenerateNextSteps(CandidateComparison candidate)
    {
        var steps = new List<string>();
        
        switch (candidate.Recommendation)
        {
            case HiringRecommendation.HighlyRecommended:
                steps.Add("Programar entrevista técnica inmediatamente");
                steps.Add("Preparar oferta competitiva");
                break;
            case HiringRecommendation.Recommended:
                steps.Add("Realizar entrevista inicial");
                steps.Add("Evaluar fit cultural");
                break;
            case HiringRecommendation.Consider:
                steps.Add("Entrevista de screening");
                steps.Add("Evaluar habilidades faltantes");
                break;
            default:
                steps.Add("Archivar perfil");
                break;
        }
        
        return steps;
    }

    #endregion
}