using CVProcessing.Core.Entities;
using CVProcessing.Core.Enums;

namespace CVProcessing.Application.DTOs;

/// <summary>
/// Request para crear una nueva sesión
/// </summary>
public record CreateSessionRequest
{
    public required JobOfferDto JobOffer { get; init; }
}

/// <summary>
/// Response de sesión creada
/// </summary>
public record CreateSessionResponse
{
    public required Guid SessionId { get; init; }
    public required SessionStatus Status { get; init; }
    public required DateTime CreatedAt { get; init; }
}

/// <summary>
/// Response con estado de sesión
/// </summary>
public record SessionStatusResponse
{
    public required Guid SessionId { get; init; }
    public required SessionStatus Status { get; init; }
    public required int TotalDocuments { get; init; }
    public required int ProcessedDocuments { get; init; }
    public required int Progress { get; init; }
    public string? StatusMessage { get; init; }
    public TimeSpan? EstimatedTimeRemaining { get; init; }
    public SessionStatisticsDto? Statistics { get; init; }
    public JobOfferDto? JobOffer { get; init; }
}

/// <summary>
/// DTO para oferta laboral
/// </summary>
public record JobOfferDto
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required List<string> RequiredSkills { get; init; } = [];
    public List<string> PreferredSkills { get; init; } = [];
    public int MinExperienceYears { get; init; }
    public string? EducationLevel { get; init; }
    public string? Location { get; init; }
    public string? WorkMode { get; init; }
    public SalaryRangeDto? SalaryRange { get; init; }
}

/// <summary>
/// DTO para rango salarial
/// </summary>
public record SalaryRangeDto
{
    public decimal Min { get; init; }
    public decimal Max { get; init; }
    public required string Currency { get; init; }
}

/// <summary>
/// DTO para estadísticas de sesión
/// </summary>
public record SessionStatisticsDto
{
    public int TotalDocuments { get; init; }
    public int ProcessedDocuments { get; init; }
    public int FailedDocuments { get; init; }
    public double AverageScore { get; init; }
    public int HighestScore { get; init; }
    public int LowestScore { get; init; }
}

/// <summary>
/// Response con lista paginada de sesiones
/// </summary>
public record SessionListResponse
{
    public required List<SessionSummaryDto> Sessions { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}

/// <summary>
/// DTO resumido de sesión para listas
/// </summary>
public record SessionSummaryDto
{
    public required Guid Id { get; init; }
    public required string JobTitle { get; init; }
    public required SessionStatus Status { get; init; }
    public required int DocumentCount { get; init; }
    public required int Progress { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
