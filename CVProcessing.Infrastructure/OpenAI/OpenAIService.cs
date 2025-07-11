using CVProcessing.Core.Entities;
using CVProcessing.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.Text.Json;

namespace CVProcessing.Infrastructure.OpenAI;

/// <summary>
/// Servicio de integración con OpenAI para análisis de CVs
/// </summary>
public class OpenAIService : IOpenAIService
{
    private readonly OpenAIClient _client;
    private readonly OpenAIConfiguration _config;
    private readonly ILogger<OpenAIService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public OpenAIService(IOptions<OpenAIConfiguration> config, ILogger<OpenAIService> logger)
    {
        _config = config.Value;
        _logger = logger;
        _client = new OpenAIClient(_config.ApiKey);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<CVData> ExtractCVDataAsync(string documentText, JobOffer jobOffer)
    {
        var prompt = PromptTemplates.ExtractCVData(documentText, jobOffer);
        
        _logger.LogInformation("Extracting CV data for job: {JobTitle}", jobOffer.Title);
        
        var response = await CallOpenAIAsync(prompt);
        
        try
        {
            var cvData = JsonSerializer.Deserialize<CVData>(response, _jsonOptions);
            if (cvData == null)
                throw new InvalidOperationException("Failed to deserialize CV data");
            
            _logger.LogInformation("CV data extracted successfully for: {Name}", cvData.PersonalInfo.Name);
            return cvData;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse OpenAI response as JSON: {Response}", response);
            throw new InvalidOperationException("Invalid JSON response from OpenAI", ex);
        }
    }

    public async Task<CandidateComparison> GenerateComparisonAsync(CVData cvData, JobOffer jobOffer)
    {
        var prompt = PromptTemplates.GenerateComparison(cvData, jobOffer);
        
        _logger.LogInformation("Generating comparison for candidate: {Name}", cvData.PersonalInfo.Name);
        
        var response = await CallOpenAIAsync(prompt);
        
        try
        {
            var comparisonData = JsonSerializer.Deserialize<ComparisonResponse>(response, _jsonOptions);
            if (comparisonData == null)
                throw new InvalidOperationException("Failed to deserialize comparison data");
            
            var comparison = new CandidateComparison
            {
                DocumentId = Guid.NewGuid(), // This should be set by the caller
                Name = cvData.PersonalInfo.Name,
                Email = cvData.PersonalInfo.Email,
                OverallScore = cvData.Score.Overall,
                Scores = cvData.Score,
                MatchingSkills = comparisonData.MatchingSkills,
                MissingSkills = comparisonData.MissingSkills,
                Strengths = comparisonData.Strengths,
                Weaknesses = comparisonData.Weaknesses,
                Recommendation = comparisonData.Recommendation,
                RelevantExperienceYears = comparisonData.RelevantExperienceYears
            };
            
            return comparison;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse comparison response: {Response}", response);
            throw new InvalidOperationException("Invalid comparison response from OpenAI", ex);
        }
    }

    public async Task<string> GenerateExecutiveSummaryAsync(CVData cvData, JobOffer jobOffer)
    {
        var prompt = PromptTemplates.GenerateExecutiveSummary(cvData, jobOffer);
        
        _logger.LogInformation("Generating executive summary for: {Name}", cvData.PersonalInfo.Name);
        
        var summary = await CallOpenAIAsync(prompt);
        return summary.Trim();
    }

    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            var response = await CallOpenAIAsync("Responde solo con 'OK'");
            return response.Contains("OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI health check failed");
            return false;
        }
    }

    public async Task<OpenAIUsageStats> GetUsageStatsAsync()
    {
        // This is a placeholder - actual implementation would track usage
        return await Task.FromResult(new OpenAIUsageStats
        {
            TokensUsed = 0,
            TokenLimit = 1000000,
            RequestCount = 0,
            EstimatedCost = 0m,
            LastReset = DateTime.UtcNow.Date
        });
    }

    private async Task<string> CallOpenAIAsync(string prompt)
    {
        var retryCount = 0;
        
        while (retryCount <= _config.MaxRetries)
        {
            try
            {
                var chatClient = _client.GetChatClient(_config.Model);
                
                var response = await chatClient.CompleteChatAsync(
                    new ChatMessage[]
                    {
                        new SystemChatMessage("Eres un experto en análisis de CVs y reclutamiento. Responde siempre en el formato solicitado."),
                        new UserChatMessage(prompt)
                    },
                    new ChatCompletionOptions
                    {
                        MaxOutputTokenCount = _config.MaxTokens,
                        Temperature = (float)_config.Temperature
                    });
                
                var content = response.Value.Content[0].Text;
                
                _logger.LogInformation("OpenAI request completed successfully");
                return content;
            }
            catch (Exception ex) when (retryCount < _config.MaxRetries)
            {
                retryCount++;
                _logger.LogWarning(ex, "OpenAI request failed, retry {RetryCount}/{MaxRetries}", retryCount, _config.MaxRetries);
                
                await Task.Delay(_config.RetryDelayMs * retryCount);
            }
        }
        
        throw new InvalidOperationException($"OpenAI request failed after {_config.MaxRetries} retries");
    }
}

/// <summary>
/// Respuesta de comparación de OpenAI
/// </summary>
internal record ComparisonResponse
{
    public List<string> MatchingSkills { get; init; } = [];
    public List<string> MissingSkills { get; init; } = [];
    public List<string> Strengths { get; init; } = [];
    public List<string> Weaknesses { get; init; } = [];
    public HiringRecommendation Recommendation { get; init; }
    public int RelevantExperienceYears { get; init; }
}