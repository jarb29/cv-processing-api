namespace CVProcessing.Infrastructure.OpenAI;

/// <summary>
/// Configuración para el servicio de OpenAI
/// </summary>
public class OpenAIConfiguration
{
    public const string SectionName = "OpenAI";
    
    /// <summary>
    /// API Key de OpenAI
    /// </summary>
    public required string ApiKey { get; init; }
    
    /// <summary>
    /// Modelo a utilizar (default: gpt-4o-mini)
    /// </summary>
    public string Model { get; init; } = "gpt-4o-mini";
    
    /// <summary>
    /// Máximo número de tokens en la respuesta
    /// </summary>
    public int MaxTokens { get; init; } = 4000;
    
    /// <summary>
    /// Temperatura para la generación (0.0 - 2.0)
    /// </summary>
    public double Temperature { get; init; } = 0.1;
    
    /// <summary>
    /// Timeout para requests en segundos
    /// </summary>
    public int TimeoutSeconds { get; init; } = 120;
    
    /// <summary>
    /// Número máximo de reintentos
    /// </summary>
    public int MaxRetries { get; init; } = 3;
    
    /// <summary>
    /// Delay entre reintentos en milisegundos
    /// </summary>
    public int RetryDelayMs { get; init; } = 1000;
}