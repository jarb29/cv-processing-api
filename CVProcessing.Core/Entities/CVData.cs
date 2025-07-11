namespace CVProcessing.Core.Entities;

/// <summary>
/// Información estructurada extraída de un CV
/// </summary>
public record CVData
{
    /// <summary>
    /// Información personal del candidato
    /// </summary>
    public required PersonalInfo PersonalInfo { get; init; }
    
    /// <summary>
    /// Experiencia laboral
    /// </summary>
    public List<Experience> Experience { get; init; } = [];
    
    /// <summary>
    /// Habilidades técnicas y blandas
    /// </summary>
    public List<string> Skills { get; init; } = [];
    
    /// <summary>
    /// Formación académica
    /// </summary>
    public List<Education> Education { get; init; } = [];
    
    /// <summary>
    /// Certificaciones y cursos
    /// </summary>
    public List<Certification> Certifications { get; init; } = [];
    
    /// <summary>
    /// Idiomas que maneja
    /// </summary>
    public List<Language> Languages { get; init; } = [];
    
    /// <summary>
    /// Puntuación general del CV (0-100)
    /// </summary>
    public CVScore Score { get; init; } = new();
    
    /// <summary>
    /// Fecha de extracción de los datos
    /// </summary>
    public DateTime ExtractedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Información personal del candidato
/// </summary>
public record PersonalInfo
{
    public required string Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Location { get; init; }
    public string? LinkedIn { get; init; }
    public string? Summary { get; init; }
}

/// <summary>
/// Experiencia laboral
/// </summary>
public record Experience
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
/// Formación académica
/// </summary>
public record Education
{
    public required string Institution { get; init; }
    public required string Degree { get; init; }
    public string? Field { get; init; }
    public int? Year { get; init; }
    public string? Grade { get; init; }
}

/// <summary>
/// Certificación o curso
/// </summary>
public record Certification
{
    public required string Name { get; init; }
    public required string Issuer { get; init; }
    public DateTime? IssueDate { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public string? CredentialId { get; init; }
}

/// <summary>
/// Idioma y nivel
/// </summary>
public record Language
{
    public required string Name { get; init; }
    public required string Level { get; init; } // Básico, Intermedio, Avanzado, Nativo
}

/// <summary>
/// Puntuación detallada del CV
/// </summary>
public record CVScore
{
    /// <summary>
    /// Puntuación general (0-100)
    /// </summary>
    public int Overall { get; init; }
    
    /// <summary>
    /// Puntuación por experiencia (0-100)
    /// </summary>
    public int Experience { get; init; }
    
    /// <summary>
    /// Puntuación por habilidades (0-100)
    /// </summary>
    public int Skills { get; init; }
    
    /// <summary>
    /// Puntuación por educación (0-100)
    /// </summary>
    public int Education { get; init; }
    
    /// <summary>
    /// Puntuación de coincidencia con la oferta laboral (0-100)
    /// </summary>
    public int JobMatch { get; init; }
}