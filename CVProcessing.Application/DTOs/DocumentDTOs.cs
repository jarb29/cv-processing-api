using CVProcessing.Core.Entities;
using CVProcessing.Core.Enums;

namespace CVProcessing.Application.DTOs;

/// <summary>
/// Response de documento subido
/// </summary>
public record UploadDocumentResponse
{
    public required Guid SessionId { get; init; }
    public required List<DocumentUploadResult> UploadedDocuments { get; init; }
    public required int TotalUploaded { get; init; }
}

/// <summary>
/// Resultado de subida de un documento individual
/// </summary>
public record DocumentUploadResult
{
    public required Guid DocumentId { get; init; }
    public required string FileName { get; init; }
    public required long Size { get; init; }
    public required DocumentStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Response con detalles completos de un documento
/// </summary>
public record DocumentDetailsResponse
{
    public required Guid DocumentId { get; init; }
    public required string FileName { get; init; }
    public required DocumentStatus Status { get; init; }
    public required long FileSize { get; init; }
    public required DateTime UploadedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public long? ProcessingTimeMs { get; init; }
    public CVDataDto? ExtractedData { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// DTO para datos extraídos del CV
/// </summary>
public record CVDataDto
{
    public required PersonalInfoDto PersonalInfo { get; init; }
    public List<ExperienceDto> Experience { get; init; } = [];
    public List<string> Skills { get; init; } = [];
    public List<EducationDto> Education { get; init; } = [];
    public List<CertificationDto> Certifications { get; init; } = [];
    public List<LanguageDto> Languages { get; init; } = [];
    public required CVScoreDto Score { get; init; }
}

/// <summary>
/// DTO para información personal
/// </summary>
public record PersonalInfoDto
{
    public required string Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Location { get; init; }
    public string? LinkedIn { get; init; }
    public string? Summary { get; init; }
}

/// <summary>
/// DTO para experiencia laboral
/// </summary>
public record ExperienceDto
{
    public required string Company { get; init; }
    public required string Position { get; init; }
    public string? Duration { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public List<string> Responsibilities { get; init; } = [];
    public List<string> Technologies { get; init; } = [];
    public bool IsCurrent { get; init; }
}

/// <summary>
/// DTO para educación
/// </summary>
public record EducationDto
{
    public required string Institution { get; init; }
    public required string Degree { get; init; }
    public string? Field { get; init; }
    public int? Year { get; init; }
    public string? Grade { get; init; }
}

/// <summary>
/// DTO para certificación
/// </summary>
public record CertificationDto
{
    public required string Name { get; init; }
    public required string Issuer { get; init; }
    public DateTime? IssueDate { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public string? CredentialId { get; init; }
}

/// <summary>
/// DTO para idioma
/// </summary>
public record LanguageDto
{
    public required string Name { get; init; }
    public required string Level { get; init; }
}

/// <summary>
/// DTO para puntuación del CV
/// </summary>
public record CVScoreDto
{
    public int Overall { get; init; }
    public int Experience { get; init; }
    public int Skills { get; init; }
    public int Education { get; init; }
    public int JobMatch { get; init; }
}