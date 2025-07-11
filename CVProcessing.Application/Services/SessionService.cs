using CVProcessing.Application.DTOs;
using CVProcessing.Core.Entities;
using CVProcessing.Core.Enums;
using CVProcessing.Core.Interfaces;
using CVProcessing.Infrastructure.Storage;
using Microsoft.Extensions.Logging;

namespace CVProcessing.Application.Services;

/// <summary>
/// Servicio de aplicación para gestión de sesiones
/// </summary>
public class SessionService : ISessionService
{
    private readonly SessionRepository _sessionRepository;
    private readonly ILogger<SessionService> _logger;

    public SessionService(SessionRepository sessionRepository, ILogger<SessionService> logger)
    {
        _sessionRepository = sessionRepository;
        _logger = logger;
    }

    public async Task<Session> CreateAsync(JobOffer jobOffer)
    {
        _logger.LogInformation("Creating new session for job: {JobTitle}", jobOffer.Title);
        
        var session = new Session
        {
            JobOffer = jobOffer,
            Status = SessionStatus.Created,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _sessionRepository.SaveAsync(session);
        
        _logger.LogInformation("Session created successfully: {SessionId}", session.Id);
        return session;
    }

    public async Task<Session?> GetByIdAsync(Guid sessionId)
    {
        _logger.LogDebug("Retrieving session: {SessionId}", sessionId);
        return await _sessionRepository.GetByIdAsync(sessionId);
    }

    public async Task UpdateStatusAsync(Guid sessionId, SessionStatus status, string? message = null)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found for status update: {SessionId}", sessionId);
            return;
        }

        session.Status = status;
        session.StatusMessage = message;
        session.UpdatedAt = DateTime.UtcNow;

        if (status == SessionStatus.Completed)
        {
            session.CompletedAt = DateTime.UtcNow;
            session.Progress = 100;
        }

        await _sessionRepository.UpdateAsync(session);
        
        _logger.LogInformation("Session status updated: {SessionId} -> {Status}", sessionId, status);
    }

    public async Task UpdateProgressAsync(Guid sessionId, int progress)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found for progress update: {SessionId}", sessionId);
            return;
        }

        session.Progress = Math.Clamp(progress, 0, 100);
        session.UpdatedAt = DateTime.UtcNow;

        await _sessionRepository.UpdateAsync(session);
        
        _logger.LogDebug("Session progress updated: {SessionId} -> {Progress}%", sessionId, progress);
    }

    public async Task<(List<Session> Sessions, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20)
    {
        _logger.LogDebug("Retrieving sessions page {Page}, size {PageSize}", page, pageSize);
        
        var allSessions = await _sessionRepository.GetAllAsync();
        var totalCount = allSessions.Count;
        
        var sessions = allSessions
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (sessions, totalCount);
    }

    public async Task DeleteAsync(Guid sessionId)
    {
        _logger.LogInformation("Deleting session: {SessionId}", sessionId);
        await _sessionRepository.DeleteAsync(sessionId);
    }

    public async Task<bool> ExistsAsync(Guid sessionId)
    {
        return await _sessionRepository.ExistsAsync(sessionId);
    }

    /// <summary>
    /// Crear sesión desde DTO
    /// </summary>
    public async Task<CreateSessionResponse> CreateFromDtoAsync(CreateSessionRequest request)
    {
        var jobOffer = MapJobOfferFromDto(request.JobOffer);
        var session = await CreateAsync(jobOffer);

        return new CreateSessionResponse
        {
            SessionId = session.Id,
            Status = session.Status,
            CreatedAt = session.CreatedAt
        };
    }

    /// <summary>
    /// Obtener estado de sesión como DTO
    /// </summary>
    public async Task<SessionStatusResponse?> GetStatusAsync(Guid sessionId)
    {
        var session = await GetByIdAsync(sessionId);
        if (session == null) return null;

        var processedCount = session.Documents.Count(d => d.Status == DocumentStatus.Processed);
        
        return new SessionStatusResponse
        {
            SessionId = session.Id,
            Status = session.Status,
            TotalDocuments = session.Documents.Count,
            ProcessedDocuments = processedCount,
            Progress = session.Progress,
            StatusMessage = session.StatusMessage,
            Statistics = MapStatisticsToDto(session.Statistics)
        };
    }

    private static JobOffer MapJobOfferFromDto(JobOfferDto dto)
    {
        return new JobOffer
        {
            Title = dto.Title,
            Description = dto.Description,
            RequiredSkills = dto.RequiredSkills,
            PreferredSkills = dto.PreferredSkills,
            MinExperienceYears = dto.MinExperienceYears,
            EducationLevel = dto.EducationLevel,
            Location = dto.Location,
            WorkMode = dto.WorkMode,
            SalaryRange = dto.SalaryRange != null ? new SalaryRange
            {
                Min = dto.SalaryRange.Min,
                Max = dto.SalaryRange.Max,
                Currency = dto.SalaryRange.Currency
            } : null
        };
    }

    private static SessionStatisticsDto MapStatisticsToDto(SessionStatistics stats)
    {
        return new SessionStatisticsDto
        {
            TotalDocuments = stats.TotalDocuments,
            ProcessedDocuments = stats.ProcessedDocuments,
            FailedDocuments = stats.FailedDocuments,
            AverageScore = stats.AverageScore,
            HighestScore = stats.HighestScore,
            LowestScore = stats.LowestScore
        };
    }
}