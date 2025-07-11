namespace CVProcessing.Core.Entities;

/// <summary>
/// Representa una oferta laboral contra la cual se compararán los CVs
/// </summary>
public record JobOffer
{
    /// <summary>
    /// Título del puesto
    /// </summary>
    public required string Title { get; init; }
    
    /// <summary>
    /// Descripción detallada del puesto
    /// </summary>
    public required string Description { get; init; }
    
    /// <summary>
    /// Habilidades requeridas
    /// </summary>
    public required List<string> RequiredSkills { get; init; } = [];
    
    /// <summary>
    /// Habilidades deseables (no obligatorias)
    /// </summary>
    public List<string> PreferredSkills { get; init; } = [];
    
    /// <summary>
    /// Años de experiencia mínimos requeridos
    /// </summary>
    public int MinExperienceYears { get; init; }
    
    /// <summary>
    /// Nivel educativo requerido
    /// </summary>
    public string? EducationLevel { get; init; }
    
    /// <summary>
    /// Ubicación del trabajo
    /// </summary>
    public string? Location { get; init; }
    
    /// <summary>
    /// Modalidad de trabajo (remoto, presencial, híbrido)
    /// </summary>
    public string? WorkMode { get; init; }
    
    /// <summary>
    /// Rango salarial (opcional)
    /// </summary>
    public SalaryRange? SalaryRange { get; init; }
    
    /// <summary>
    /// Fecha de creación de la oferta
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Representa un rango salarial
/// </summary>
public record SalaryRange
{
    public decimal Min { get; init; }
    public decimal Max { get; init; }
    public required string Currency { get; init; }
}